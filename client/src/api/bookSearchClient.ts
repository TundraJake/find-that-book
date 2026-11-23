import type { SearchResult } from '../types.ts'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
  quota_scope?: QuotaScope
  retry_after_seconds?: number
  source?: string
}

export type QuotaScope = 'daily' | 'per_minute'

/** Every Gemini model is over quota (API answered 429 with quota_scope). */
export interface QuotaExhaustion {
  scope: QuotaScope
  retryAfterSeconds: number
}

export class BookSearchError extends Error {
  readonly quotaExhaustion: QuotaExhaustion | null
  /** The API refused the query as unsafe (422): safety filters or explanation output checks. */
  readonly isRejected: boolean
  /** Seconds to wait when every Gemini model is overloaded (503 "high demand"); null for other errors. */
  readonly busyRetryAfterSeconds: number | null
  /** Open Library timed out or failed (503 with source "open_library"); usually temporary, so the UI offers a retry. */
  readonly isCatalogUnavailable: boolean

  constructor(
    message: string,
    quotaExhaustion: QuotaExhaustion | null = null,
    isRejected = false,
    busyRetryAfterSeconds: number | null = null,
    isCatalogUnavailable = false,
  ) {
    super(message)
    this.quotaExhaustion = quotaExhaustion
    this.isRejected = isRejected
    this.busyRetryAfterSeconds = busyRetryAfterSeconds
    this.isCatalogUnavailable = isCatalogUnavailable
  }
}

const tooManyRequestsStatus = 429
const rejectedStatus = 422
const serviceUnavailableStatus = 503
const openLibrarySource = 'open_library'

const unreachableMessage = 'Could not reach the API. Is the .NET backend running on port 5080?'
const gatewayFailureStatuses = new Set([502, 503, 504])

async function readProblemDetails(response: Response): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}

/** Message from an API ProblemDetails body; null when the body is not ProblemDetails (e.g. the dev proxy could not reach the API). */
function describeProblem(problem: ProblemDetails | null): string | null {
  if (problem === null) return null
  const firstValidationMessage = problem.errors ? Object.values(problem.errors).flat()[0] : undefined
  if (firstValidationMessage) return firstValidationMessage
  if (problem.title && problem.detail) return `${problem.title}: ${problem.detail}`
  return problem.detail ?? problem.title ?? null
}

function readQuotaExhaustion(response: Response, problem: ProblemDetails | null): QuotaExhaustion | null {
  if (response.status !== tooManyRequestsStatus || problem?.quota_scope === undefined) return null
  return { scope: problem.quota_scope, retryAfterSeconds: problem.retry_after_seconds ?? 0 }
}

/** Calls POST /api/books/search on the .NET API (proxied by Vite in development). */
export async function searchBooks(query: string, signal: AbortSignal): Promise<SearchResult> {
  let response: Response
  try {
    response = await fetch('/api/books/search', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ query }),
      signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new BookSearchError(unreachableMessage)
  }

  if (!response.ok) {
    const problem = await readProblemDetails(response)
    const problemMessage = describeProblem(problem)
    if (problemMessage !== null) {
      const isServiceUnavailable = response.status === serviceUnavailableStatus
      const busyRetryAfterSeconds = isServiceUnavailable ? (problem?.retry_after_seconds ?? null) : null
      throw new BookSearchError(
        problemMessage,
        readQuotaExhaustion(response, problem),
        response.status === rejectedStatus,
        busyRetryAfterSeconds,
        isServiceUnavailable && problem?.source === openLibrarySource,
      )
    }
    throw new BookSearchError(gatewayFailureStatuses.has(response.status) ? unreachableMessage : `Request failed (${response.status})`)
  }
  return (await response.json()) as SearchResult
}
