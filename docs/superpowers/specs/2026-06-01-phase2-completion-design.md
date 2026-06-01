# Phase 2 Completion — Create Project Modal + Settings Update Form

**Date:** 2026-06-01  
**Scope:** Two frontend additions to close out Phase 2. No backend changes required.

---

## 1. Create Project Modal

### What

A `CreateProjectDialog` component wired to the existing "New Project" button on `/dashboard/projects`. Uses the already-implemented `useCreateProject()` mutation and `useClients()` hook.

### Component

**File:** `flowdesk-web/src/components/projects/CreateProjectDialog.tsx`

Props:
```ts
interface CreateProjectDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}
```

### Form Fields

| Field | Type | Validation |
|---|---|---|
| `name` | text input | required, min 1 char |
| `description` | textarea | optional |
| `clientId` | select | required; options from `useClients()` |

Zod schema:
```ts
z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().nullable().optional(),
  clientId: z.string().min(1, "Client is required"),
})
```

### Behaviour

- **Empty client list:** If `useClients()` returns `[]`, show inline message "No clients yet — invite a client first" and disable the submit button.
- **Success:** Close dialog, show success toast. `useCreateProject()` already invalidates `["projects"]` on success.
- **Error:** Show toast with the API's `detail` field (from `ApiError`), keep dialog open.
- **Loading state:** Submit button shows spinner and is disabled while mutation is pending.

### Integration into Projects Page

`ProjectsPage` gains local `open` state. The "New Project" `<Button>` toggles it. `CreateProjectDialog` is rendered at the bottom of the page JSX. The page also migrates its inline `useQuery` call to the `useProjects()` hook from `lib/queries.ts` (currently duplicated — small cleanup, same scope).

---

## 2. Settings Update Form

### What

Replace the read-only Agency card on `/dashboard/settings` with an editable form. Profile card stays read-only (name/email/role come from the JWT and can't be changed here).

### Data source change

Currently the page reads `user.organisationName` from `AuthContext` (stale after any update). Switch the Agency card to load from `useOrganisation()` so it always reflects server state.

### Form Fields

| Field | Type | Validation |
|---|---|---|
| `name` | text input | required, min 1 char |
| `primaryColor` | `<input type="color">` | optional; defaults to `#7c3aed` if null |

Zod schema:
```ts
z.object({
  name: z.string().min(1, "Agency name is required"),
  primaryColor: z.string().nullable(),
})
```

### Behaviour

- Form initialises with values from `useOrganisation()` via `react-hook-form` `reset()` once data loads.
- Save and Cancel buttons render below the fields; both disabled until the form is dirty (`formState.isDirty`).
- **Save:** Calls `useUpdateOrganisation()`. On success: show toast, call `reset(newValues)` to clear dirty state. On error: show toast, keep form dirty.
- **Cancel:** Calls `reset()` to restore last-saved values.
- Remove the placeholder text "Full agency settings (branding, custom domain, team members) are coming in Phase 2."

### No backend changes

`UpdateOrganisationRequest` already accepts `{ name, primaryColor }` and the API + service are implemented.

---

## Out of Scope

- Invoices (Phase 4)
- Slug / CustomDomain / Plan fields in settings (Phase 5)
- Team members management
- Delete account
- Deliverable delete endpoint
