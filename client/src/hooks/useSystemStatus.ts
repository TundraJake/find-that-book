import { useCallback, useEffect, useState } from 'react'
import { fetchSystemStatus } from '../api/systemStatusClient.ts'

export type SystemReadiness = { state: 'checking' } | { state: 'ready' } | { state: 'notConfigured'; problems: string[] } | { state: 'unreachable' }

/** Loads GET /api/status once on mount; `recheck` runs it again (e.g. after the API restarts). */
export function useSystemStatus() {
  const [readiness, setReadiness] = useState<SystemReadiness>({ state: 'checking' })
  const [checkCount, setCheckCount] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    fetchSystemStatus(controller.signal)
      .then((status) => setReadiness(status.ready ? { state: 'ready' } : { state: 'notConfigured', problems: status.problems }))
      .catch(() => {
        if (!controller.signal.aborted) setReadiness({ state: 'unreachable' })
      })
    return () => controller.abort()
  }, [checkCount])

  const recheck = useCallback(() => {
    setReadiness({ state: 'checking' })
    setCheckCount((count) => count + 1)
  }, [])

  return { readiness, recheck }
}
