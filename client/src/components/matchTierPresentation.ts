import type { MatchTier } from '../types.ts'

interface MatchTierPresentation {
  label: string
  color: 'success' | 'primary' | 'info' | 'warning' | 'default'
}

const presentations: Record<MatchTier, MatchTierPresentation> = {
  exact_title_primary_author: { label: 'Best match', color: 'success' },
  exact_title_contributor_author: { label: 'Title match · contributor', color: 'info' },
  near_title_author: { label: 'Close match', color: 'primary' },
  author_only: { label: 'Same author', color: 'warning' },
  fallback: { label: 'Possible match', color: 'default' },
}

export function getMatchTierPresentation(matchTier: MatchTier): MatchTierPresentation {
  return presentations[matchTier]
}

/** Strongest first — mirrors the backend MatchTier enum order. */
export const matchTierOrder: MatchTier[] = [
  'exact_title_primary_author',
  'exact_title_contributor_author',
  'near_title_author',
  'author_only',
  'fallback',
]
