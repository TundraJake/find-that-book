import { useEffect, useState } from 'react'
import { fetchAiUsage, type AiUsage } from '../api/usageClient.ts'

/** Refresh often enough that the rolling 60-second count visibly ticks down. */
const pollIntervalMilliseconds = 5000

/**
 * Polls GET /api/usage. `refreshKey` forces an immediate refresh (e.g. after a search finishes).
 * Returns null until the first successful response, or while the API is unreachable.
 */
export function useAiUsage(refreshKey: number): AiUsage | null {
  const [usage, setUsage] = useState<AiUsage | null>(null)

  useEffect(() => {
    let controller = new AbortController()

    function load() {
      controller.abort()
      controller = new AbortController()
      fetchAiUsage(controller.signal)
        .then(setUsage)
        .catch(() => {
          if (!controller.signal.aborted) setUsage(null)
        })
    }

    load()
    const timer = window.setInterval(load, pollIntervalMilliseconds)
    return () => {
      window.clearInterval(timer)
      controller.abort()
    }
  }, [refreshKey])

  return usage
}
