using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Notifications;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class NotificationServiceTests
{
    [Fact]
    public async Task Inbox_is_recipient_scoped_newest_first_and_paginated()
    {
        var databaseName = Guid.NewGuid().ToString();
        var owner = User(RoleNames.CompanyAdmin);
        var other = User(RoleNames.CompanyAdmin);
        await SeedNotificationsAsync(databaseName, owner.UserId!.Value, other.UserId!.Value, countForOwner: 21);

        await using var dbContext = CreateContext(databaseName, owner);
        var page = await CreateService(dbContext, owner).GetUserNotificationsAsync(new NotificationListQuery { Page = 1, PageSize = 10 });

        page.TotalCount.Should().Be(21);
        page.Items.Should().HaveCount(10);
        page.Items.Select(item => item.CreatedAt).Should().BeInDescendingOrder();
        page.Items.Should().OnlyContain(item => item.Title.StartsWith("Owner"));
    }

    [Fact]
    public async Task Unread_count_and_mark_as_read_only_affect_current_user()
    {
        var databaseName = Guid.NewGuid().ToString();
        var owner = User(RoleNames.SalesEmployee);
        var other = User(RoleNames.SalesEmployee);
        var ids = await SeedNotificationsAsync(databaseName, owner.UserId!.Value, other.UserId!.Value, countForOwner: 2);

        await using (var dbContext = CreateContext(databaseName, owner))
        {
            var service = CreateService(dbContext, owner);
            (await service.GetUnreadCountAsync()).Should().Be(2);
            var marked = await service.MarkAsReadAsync(ids.OwnerFirst);
            marked!.IsRead.Should().BeTrue();
            marked.ReadAt.Should().NotBeNull();
            (await service.GetUnreadCountAsync()).Should().Be(1);
        }

        await using (var otherContext = CreateContext(databaseName, other))
        {
            var otherService = CreateService(otherContext, other);
            (await otherService.GetUnreadCountAsync()).Should().Be(2);
            (await otherService.MarkAsReadAsync(ids.OwnerFirst)).Should().BeNull();
        }
    }

    [Fact]
    public async Task Mark_all_as_read_does_not_change_another_users_notifications()
    {
        var databaseName = Guid.NewGuid().ToString();
        var owner = User(RoleNames.DispatchUser);
        var other = User(RoleNames.DispatchUser);
        await SeedNotificationsAsync(databaseName, owner.UserId!.Value, other.UserId!.Value, countForOwner: 3);

        await using (var dbContext = CreateContext(databaseName, owner))
        {
            (await CreateService(dbContext, owner).MarkAllAsReadAsync()).Should().Be(3);
            (await CreateService(dbContext, owner).GetUnreadCountAsync()).Should().Be(0);
        }

        await using var otherContext = CreateContext(databaseName, other);
            (await CreateService(otherContext, other).GetUnreadCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task User_cannot_read_another_users_notification_by_id()
    {
        var databaseName = Guid.NewGuid().ToString();
        var owner = User(RoleNames.CompanyAdmin);
        var other = User(RoleNames.SalesEmployee);
        var ids = await SeedNotificationsAsync(databaseName, owner.UserId!.Value, other.UserId!.Value, countForOwner: 1);

        await using var dbContext = CreateContext(databaseName, other);
        (await CreateService(dbContext, other).GetByIdAsync(ids.OwnerFirst)).Should().BeNull();
    }

    [Fact]
    public async Task Company_admin_cannot_read_another_company_notification()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCompaniesAsync(databaseName);
        var jeekoAdminId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        await using (var seed = CreateContext(databaseName, SystemCurrentUser.Instance))
        {
            seed.Notifications.Add(Notification(
                notificationId,
                SeedIdentifiers.JeekoCompanyId,
                jeekoAdminId,
                "Jeeko only",
                DateTime.UtcNow));
            await seed.SaveChangesAsync();
        }

        var gravisAdmin = User(RoleNames.CompanyAdmin, SeedIdentifiers.GravisCompanyId);
        await using var dbContext = CreateContext(databaseName, gravisAdmin);
        (await CreateService(dbContext, gravisAdmin).GetByIdAsync(notificationId)).Should().BeNull();
        (await CreateService(dbContext, gravisAdmin).GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SuperAdmin_sees_own_notifications_across_companies_but_not_other_inboxes()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCompaniesAsync(databaseName);
        var superAdmin = User(RoleNames.SuperAdmin, companyId: null);
        var admin = User(RoleNames.CompanyAdmin);
        await using (var seed = CreateContext(databaseName, SystemCurrentUser.Instance))
        {
            seed.Notifications.AddRange(
                Notification(Guid.NewGuid(), SeedIdentifiers.GravisCompanyId, superAdmin.UserId!.Value, "Gravis event", DateTime.UtcNow.AddMinutes(-2)),
                Notification(Guid.NewGuid(), SeedIdentifiers.JeekoCompanyId, superAdmin.UserId.Value, "Jeeko event", DateTime.UtcNow.AddMinutes(-1)),
                Notification(Guid.NewGuid(), SeedIdentifiers.GravisCompanyId, admin.UserId!.Value, "Admin only", DateTime.UtcNow));
            await seed.SaveChangesAsync();
        }

        await using var dbContext = CreateContext(databaseName, superAdmin);
        var inbox = await CreateService(dbContext, superAdmin).GetUserNotificationsAsync(new NotificationListQuery());
        inbox.Items.Should().HaveCount(2);
        inbox.Items.Select(item => item.Title).Should().BeEquivalentTo("Jeeko event", "Gravis event");
    }

    [Fact]
    public async Task Duplicate_recipients_and_duplicate_events_create_a_single_notification()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedCompaniesAsync(databaseName);
        var sharedId = Guid.NewGuid();
        var directory = new TestUserDirectory()
            .With(sharedId, RoleNames.CompanyAdmin, SeedIdentifiers.GravisCompanyId, "Admin")
            .With(sharedId, RoleNames.SuperAdmin, null, "Admin");
        var actor = User(RoleNames.SalesEmployee);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CompanyId = SeedIdentifiers.GravisCompanyId,
            OrderNumber = "GRAVIS-20260915-0001",
            CustomerName = "Jay Jalaram Electronics",
            CreatedByUserId = actor.UserId
        };

        await using var dbContext = CreateContext(databaseName, actor);
        var service = new NotificationService(dbContext, actor, directory);
        await service.NotifyOrderCreatedAsync(order);
        await service.NotifyOrderCreatedAsync(order);
        await dbContext.SaveChangesAsync();

        var stored = await dbContext.Notifications.IgnoreQueryFilters().ToListAsync();
        stored.Should().ContainSingle();
        stored[0].RecipientUserId.Should().Be(sharedId);
        stored[0].Type.Should().Be(NotificationType.OrderCreated);
    }

    [Fact]
    public async Task Recent_does_not_mark_notifications_as_read()
    {
        var databaseName = Guid.NewGuid().ToString();
        var owner = User(RoleNames.CompanyAdmin);
        await SeedNotificationsAsync(databaseName, owner.UserId!.Value, Guid.NewGuid(), countForOwner: 3);
        await using var dbContext = CreateContext(databaseName, owner);
        var service = CreateService(dbContext, owner);
        var recent = await service.GetRecentAsync(2);
        recent.Should().HaveCount(2);
        (await service.GetUnreadCountAsync()).Should().Be(3);
    }

    private static NotificationService CreateService(ApplicationDbContext dbContext, ICurrentUser currentUser) =>
        new(dbContext, currentUser, new TestUserDirectory());

    private static TestCurrentUser User(string role, Guid? companyId = null) => new()
    {
        CompanyId = role == RoleNames.SuperAdmin ? companyId : companyId ?? SeedIdentifiers.GravisCompanyId,
        Roles = [role],
        UserId = Guid.NewGuid()
    };

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options, currentUser);

    private static async Task SeedCompaniesAsync(string databaseName)
    {
        await using var dbContext = CreateContext(databaseName, SystemCurrentUser.Instance);
        if (!await dbContext.Companies.AnyAsync())
        {
            dbContext.Companies.AddRange(
                new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
                new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task<(Guid OwnerFirst, Guid OtherFirst)> SeedNotificationsAsync(
        string databaseName,
        Guid ownerId,
        Guid otherId,
        int countForOwner)
    {
        await SeedCompaniesAsync(databaseName);
        await using var dbContext = CreateContext(databaseName, SystemCurrentUser.Instance);
        Guid? ownerFirst = null;
        Guid? otherFirst = null;
        for (var index = 0; index < countForOwner; index++)
        {
            var ownerNotification = Notification(
                Guid.NewGuid(),
                SeedIdentifiers.GravisCompanyId,
                ownerId,
                $"Owner {index}",
                DateTime.UtcNow.AddMinutes(index));
            ownerFirst ??= ownerNotification.Id;
            dbContext.Notifications.Add(ownerNotification);
        }

        for (var index = 0; index < 2; index++)
        {
            var otherNotification = Notification(
                Guid.NewGuid(),
                SeedIdentifiers.GravisCompanyId,
                otherId,
                $"Other {index}",
                DateTime.UtcNow.AddMinutes(index));
            otherFirst ??= otherNotification.Id;
            dbContext.Notifications.Add(otherNotification);
        }

        await dbContext.SaveChangesAsync();
        var owned = await dbContext.Notifications
            .Where(item => item.RecipientUserId == ownerId)
            .OrderBy(item => item.Title)
            .ToListAsync();
        for (var index = 0; index < owned.Count; index++)
        {
            owned[index].CreatedAt = DateTime.UtcNow.AddMinutes(index);
        }

        await dbContext.SaveChangesAsync();
        return (ownerFirst!.Value, otherFirst!.Value);
    }

    private static Notification Notification(
        Guid id,
        Guid companyId,
        Guid recipientUserId,
        string title,
        DateTime createdAt) => new()
    {
        Id = id,
        CompanyId = companyId,
        RecipientUserId = recipientUserId,
        Type = NotificationType.OrderCreated,
        Title = title,
        Message = $"{title} message",
        RelatedEntityType = NotificationRelatedEntities.Order,
        RelatedEntityId = Guid.NewGuid(),
        EventKey = $"{id:N}",
        CreatedAt = createdAt
    };
}
