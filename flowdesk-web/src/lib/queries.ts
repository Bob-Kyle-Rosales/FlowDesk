import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import type {
  Project, ProjectStatsResponse,
  Milestone, CreateMilestoneRequest,
  Deliverable, CreateDeliverableRequest, UploadUrlResponse,
  OrganisationResponse, UpdateOrganisationRequest,
  UserSummary, CreateProjectRequest,
} from "@/types";

// ── Projects ──────────────────────────────────────────────────────────────────
export function useProjects() {
  return useQuery<Project[]>({
    queryKey: ["projects"],
    queryFn: () => api.get("/api/projects").then(r => r.data),
  });
}

export function useProject(id: string) {
  return useQuery<Project>({
    queryKey: ["projects", id],
    queryFn: () => api.get(`/api/projects/${id}`).then(r => r.data),
  });
}

export function useProjectStats(id: string) {
  return useQuery<ProjectStatsResponse>({
    queryKey: ["projects", id, "stats"],
    queryFn: () => api.get(`/api/projects/${id}/stats`).then(r => r.data),
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateProjectRequest) =>
      api.post<Project>("/api/projects", data).then(r => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["projects"] }),
  });
}

// ── Milestones ────────────────────────────────────────────────────────────────
export function useMilestones(projectId: string) {
  return useQuery<Milestone[]>({
    queryKey: ["projects", projectId, "milestones"],
    queryFn: () => api.get(`/api/projects/${projectId}/milestones`).then(r => r.data),
  });
}

export function useCreateMilestone(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateMilestoneRequest) =>
      api.post<Milestone>(`/api/projects/${projectId}/milestones`, data).then(r => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "milestones"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    },
  });
}

export function useUpdateMilestoneStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      api.patch<Milestone>(`/api/milestones/${id}/status`, { status }).then(r => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "milestones"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    },
  });
}

// ── Deliverables ──────────────────────────────────────────────────────────────
export function useDeliverables(projectId: string) {
  return useQuery<Deliverable[]>({
    queryKey: ["projects", projectId, "deliverables"],
    queryFn: () => api.get(`/api/projects/${projectId}/deliverables`).then(r => r.data),
  });
}

export function useCreateDeliverable(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateDeliverableRequest) =>
      api.post<Deliverable>(`/api/projects/${projectId}/deliverables`, data).then(r => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "deliverables"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    },
  });
}

export function useGetUploadUrl() {
  return useMutation({
    mutationFn: ({ id, fileName, contentType }: { id: string; fileName: string; contentType: string }) =>
      api.post<UploadUrlResponse>(`/api/deliverables/${id}/upload-url`, { fileName, contentType }).then(r => r.data),
  });
}

export function useConfirmUpload(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, fileUrl }: { id: string; fileUrl: string }) =>
      api.patch<Deliverable>(`/api/deliverables/${id}`, { fileUrl }).then(r => r.data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "deliverables"] }),
  });
}

export function useApproveDeliverable(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      api.patch<Deliverable>(`/api/deliverables/${id}/approve`).then(r => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "deliverables"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    },
  });
}

export function useRequestRevision(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string }) =>
      api.patch<Deliverable>(`/api/deliverables/${id}/revision`, { notes }).then(r => r.data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "deliverables"] }),
  });
}

// ── Organisation ──────────────────────────────────────────────────────────────
export function useOrganisation() {
  return useQuery<OrganisationResponse>({
    queryKey: ["organisation"],
    queryFn: () => api.get("/api/organisations/me").then(r => r.data),
  });
}

export function useUpdateOrganisation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateOrganisationRequest) =>
      api.put<OrganisationResponse>("/api/organisations/me", data).then(r => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["organisation"] }),
  });
}

// ── Users (client picker) ─────────────────────────────────────────────────────
export function useClients() {
  return useQuery<UserSummary[]>({
    queryKey: ["clients"],
    queryFn: () => api.get("/api/users?role=Client").then(r => r.data),
  });
}
