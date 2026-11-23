export type MatchTier = 'exact_title_primary_author' | 'exact_title_contributor_author' | 'near_title_author' | 'author_only' | 'fallback'

export interface QueryHypothesis {
  title: string | null
  author: string | null
  keywords: string[]
  character_hints: string[]
  year_hint: number | null
}

export interface BookCandidate {
  title: string
  /** Primary author(s) joined with ", " (the single "author" field of the requested result format). */
  author: string
  primary_authors: string[]
  contributors: string[]
  first_publish_year: number | null
  work_key: string
  open_library_url: string
  cover_url: string | null
  match_tier: MatchTier
  explanation: string
  /** Gemini's own ranking (1 = most likely); null when Gemini's ranks were unusable. */
  ai_rank: number | null
}

/** The queried author's Open Library page, when they were found as a primary author. */
export interface MatchedAuthor {
  name: string
  open_library_url: string
}

export interface SearchResult {
  query: string
  hypothesis: QueryHypothesis
  candidates: BookCandidate[]
  matched_author: MatchedAuthor | null
}
