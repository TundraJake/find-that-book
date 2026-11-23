import type { BookCandidate, MatchTier } from '../types.ts'

export type TierFilter = MatchTier | 'all'
export type SortOrder = 'ranking' | 'aiRanking' | 'newest' | 'oldest' | 'title'

export const sortOrderLabels: Record<SortOrder, string> = {
  ranking: 'Best match first',
  aiRanking: 'AI ranking',
  newest: 'Newest first',
  oldest: 'Oldest first',
  title: 'Title A–Z',
}

/** A candidate paired with its position in the API's ranked list (1-based). */
export interface RankedCandidate {
  candidate: BookCandidate
  rank: number
}

/** Candidates without an AI rank go last, in best-match order. */
function compareAiRanks(left: RankedCandidate, right: RankedCandidate): number {
  const leftRank = left.candidate.ai_rank ?? Number.POSITIVE_INFINITY
  const rightRank = right.candidate.ai_rank ?? Number.POSITIVE_INFINITY
  return leftRank === rightRank ? left.rank - right.rank : leftRank - rightRank
}

/** AI ranking is offered only when Gemini ranked this result set. */
export function hasAiRanking(candidates: BookCandidate[]): boolean {
  return candidates.some((candidate) => candidate.ai_rank !== null)
}

function compareYears(left: number | null, right: number | null, direction: 1 | -1): number {
  if (left === null && right === null) return 0
  if (left === null) return 1
  if (right === null) return -1
  return (left - right) * direction
}

/** Applies the tier filter and sort order without losing each candidate's original API rank. */
export function arrangeCandidates(candidates: BookCandidate[], tierFilter: TierFilter, sortOrder: SortOrder): RankedCandidate[] {
  const ranked = candidates.map((candidate, index) => ({ candidate, rank: index + 1 }))
  const filtered = tierFilter === 'all' ? ranked : ranked.filter((item) => item.candidate.match_tier === tierFilter)

  const sorted = [...filtered]
  switch (sortOrder) {
    case 'newest':
      sorted.sort((left, right) => compareYears(left.candidate.first_publish_year, right.candidate.first_publish_year, -1) || left.rank - right.rank)
      break
    case 'oldest':
      sorted.sort((left, right) => compareYears(left.candidate.first_publish_year, right.candidate.first_publish_year, 1) || left.rank - right.rank)
      break
    case 'aiRanking':
      sorted.sort(compareAiRanks)
      break
    case 'title':
      sorted.sort((left, right) => left.candidate.title.localeCompare(right.candidate.title) || left.rank - right.rank)
      break
    case 'ranking':
      break
  }
  return sorted
}

export function countByTier(candidates: BookCandidate[]): Map<MatchTier, number> {
  const counts = new Map<MatchTier, number>()
  for (const candidate of candidates) counts.set(candidate.match_tier, (counts.get(candidate.match_tier) ?? 0) + 1)
  return counts
}
