import OpenInNewIcon from '@mui/icons-material/OpenInNew'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
import Link from '@mui/material/Link'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { KeyboardEvent, MouseEvent } from 'react'
import type { BookCandidate } from '../types.ts'
import { BookCover } from './BookCover.tsx'
import { getMatchTierPresentation } from './matchTierPresentation.ts'

interface BookCardProps {
  candidate: BookCandidate
  rank: number
  isInspected: boolean
  onInspect: () => void
}

export function BookCard({ candidate, rank, isInspected, onInspect }: BookCardProps) {
  const tier = getMatchTierPresentation(candidate.match_tier)
  const isTopResult = rank === 1 && candidate.match_tier === 'exact_title_primary_author'

  function handleClick(event: MouseEvent<HTMLDivElement>) {
    const isSelectingText = (window.getSelection()?.toString().length ?? 0) > 0
    const clickedLink = (event.target as HTMLElement).closest('a') !== null
    if (!isSelectingText && !clickedLink) onInspect()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.target !== event.currentTarget) return
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      onInspect()
    }
  }

  return (
    <Card
      tabIndex={0}
      aria-label={`${candidate.title}. Select to view JSON in the inspector`}
      aria-current={isInspected ? 'true' : undefined}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      sx={{
        cursor: 'pointer',
        outline: 'none',
        '&:focus-visible': { boxShadow: '0 0 0 3px rgba(31, 58, 95, 0.35)' },
        ...(isTopResult && { borderColor: 'success.light', borderWidth: 1.5 }),
        ...(isInspected && { borderColor: 'primary.main', borderWidth: 1.5, boxShadow: '0 0 0 3px rgba(31, 58, 95, 0.12)' }),
      }}
    >
      <CardContent sx={{ p: { xs: 2, sm: 2.5 }, '&:last-child': { pb: { xs: 2, sm: 2.5 } } }}>
        <Stack direction="row" spacing={{ xs: 2, sm: 2.5 }}>
          <BookCover coverUrl={candidate.cover_url} title={candidate.title} />

          <Stack spacing={1} sx={{ minWidth: 0, flexGrow: 1 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }} useFlexGap>
              <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1 }}>
                #{rank}
              </Typography>
              <Chip size="small" label={tier.label} color={tier.color} variant={isTopResult ? 'filled' : 'outlined'} />
            </Stack>

            <Typography variant="h3" component="h3">
              {candidate.title}
            </Typography>

            <Typography variant="body2" color="text.secondary">
              <Typography component="span" variant="body2" color="text.primary" sx={{ fontWeight: 600 }}>
                {candidate.primary_authors.join(', ')}
              </Typography>
              {candidate.first_publish_year && ` · First published ${candidate.first_publish_year}`}
              {candidate.contributors.length > 0 && ` · with ${candidate.contributors.join(', ')}`}
            </Typography>

            <Typography variant="body2" sx={{ color: 'text.primary', borderLeft: 3, borderColor: 'secondary.main', pl: 1.5, py: 0.25 }}>
              {candidate.explanation}
            </Typography>

            <Link
              href={candidate.open_library_url}
              target="_blank"
              rel="noopener noreferrer"
              variant="body2"
              underline="hover"
              sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, alignSelf: 'flex-start', fontWeight: 600 }}
            >
              View on Open Library
              <OpenInNewIcon sx={{ fontSize: 14 }} />
            </Link>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  )
}
