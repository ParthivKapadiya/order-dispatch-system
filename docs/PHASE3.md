# Phase 3 — Dispatch and order modification

## Workflow

```
Sales
  ↓
Order Received
  ↓
Modification Request?
  ├── No
  │    ↓
  │ Ready to Dispatch
  │    ↓
  │ Dispatch
  │    ↓
  │ Dispatch Done
  │    ↓
  │ LOCKED
  │
  └── Yes
       ↓
   Approval
    ├── Reject
    │    ↓
    │ Order unchanged
    │
    └── Approve
         ↓
      Order updated
         ↓
      Ready to Dispatch
```

Valid status transitions (server-side only):

- `Received` → `ReadyToDispatch`
- `ReadyToDispatch` → `DispatchDone`

No backwards transitions. `Received` cannot skip to `DispatchDone`.

## Roles

| Role | Orders | Request modification | Approve / reject | Ready to Dispatch | Dispatch |
| --- | --- | --- | --- | --- | --- |
| SuperAdmin | All companies | No | Yes | Yes | Yes |
| CompanyAdmin | Own company | No | Own company | Own company | Own company |
| SalesEmployee | Own orders only | Own orders | No | No | No |
| DispatchUser | Via dispatch screens | No | No | Yes | Own company |

## Modification process

1. Sales employee opens an order they created (`Received` or `ReadyToDispatch`).
2. They submit **Request Modification** with a reason and the requested order values.
3. The live order does **not** change.
4. Only one `Pending` request is allowed per order.
5. CompanyAdmin / SuperAdmin reviews current vs requested values.
6. **Approve** applies product snapshots, addresses, payment, transporter, booking, remarks, and bill amount in one transaction.
7. **Reject** requires a reason and leaves the order unchanged.
8. Customer identity is not editable on a modification request.

Dispatch Done orders cannot receive a new request. A pending request blocks Ready to Dispatch and Dispatch Done.

## Dispatch process

1. Authorized user marks a `Received` order **Ready to Dispatch** (POST, antiforgery, confirmation).
2. Dispatch dashboard lists Ready to Dispatch orders.
3. Dispatch user opens the order, enters date, person, transporter, LR/booking, notes, and optional photos.
4. **Mark Dispatch Done** requires dispatch details and either an LR/booking number **or** a transport receipt / LR file.
5. Material photos are supported and encouraged, not mandatory.
6. Completing dispatch sets status to `DispatchDone` and locks the order.

One completed dispatch per order.

## File upload rules

- Allowed: `.jpg`, `.jpeg`, `.png`, `.webp`, `.pdf`
- Rejected: executables and other extensions
- Max size: 10 MB per file (existing `FileStorage` config)
- Stored under `App_Data/uploads` with a generated file name
- Original name, type, uploader, and timestamp are stored on `DispatchDocument`
- Files are served through `/Dispatch/Document/{id}` after authorization, not by raw path

## Locking

When `Order.Status == DispatchDone`:

- Sales cannot request modification
- Admins cannot approve a modification against that order
- Dispatch cannot complete the same order again
- There is no unrestricted Edit action

## Authorization and isolation

Company is taken from the authenticated user (SuperAdmin selects a company where required). Posted `CompanyId`, `ProductId`, `TransporterId`, `OrderId`, and `ModificationRequestId` values are re-checked server-side. Cross-company IDs fail closed (not found or business error).

## Database changes

Migration: `Phase3DispatchModificationWorkflow`

Added/extended:

- `OrderModificationRequests`: requested-by name, current/requested JSON snapshots, reviewer, review time, rejection reason, enum status
- `Dispatches`: dispatch date, person, authenticated dispatcher, LR/booking numbers, completed flag, unique order
- `DispatchDocuments`: document type enum storage, uploaded-by user

## Assumptions

- Material photos are optional; transport receipt/LR number **or** uploaded LR/receipt is required to complete dispatch.
- CompanyAdmin and SuperAdmin do not create modification requests; they only review them.
- Sales employees see only orders they created, not every order in the company.
- Dispatch person name can be typed, but the authenticated user id is always stored.
- No WhatsApp, SMS, email, inventory, or accounting in this phase.

## Notifications

Phase 4 implements the in-app inbox. Dispatch and modification services raise notifications inside the same EF transaction as the business change. See `docs/PHASE4.md`. WhatsApp/SMS/email remain out of scope.
