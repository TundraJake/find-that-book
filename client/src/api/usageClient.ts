export interface AiUsage {
  requests_in_window: number
  requests_per_minute_limit: number
  window_seconds: number
}

/** Calls GET /api/usage. Throws when the API cannot be reached. */
export async function fetchAiUsage(signal: AbortSignal): Promise<AiUsage> {
  const response = await fetch('/api/usage', { signal })
  if (!response.ok) throw new Error(`Usage check failed (${response.status})`)
  return (await response.json()) as AiUsage
}
