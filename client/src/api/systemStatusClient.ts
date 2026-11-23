export interface SystemStatus {
  ready: boolean
  gemini_configured: boolean
  problems: string[]
}

/** Calls GET /api/status. Throws when the API cannot be reached. */
export async function fetchSystemStatus(signal: AbortSignal): Promise<SystemStatus> {
  const response = await fetch('/api/status', { signal })
  if (!response.ok) throw new Error(`Status check failed (${response.status})`)
  return (await response.json()) as SystemStatus
}
