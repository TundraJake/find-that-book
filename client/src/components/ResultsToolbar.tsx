import FilterListIcon from '@mui/icons-material/FilterList'
import Chip from '@mui/material/Chip'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import type { BookCandidate } from '../types.ts'
import { getMatchTierPresentation, matchTierOrder } from './matchTierPresentation.ts'
import { countByTier, hasAiRanking, sortOrderLabels, type SortOrder, type TierFilter } from './resultOrdering.ts'

interface ResultsToolbarProps {
  candidates: BookCandidate[]
  tierFilter: TierFilter
  sortOrder: SortOrder
  onTierFilterChange: (tierFilter: TierFilter) => void
  onSortOrderChange: (sortOrder: SortOrder) => void
}

export function ResultsToolbar({ candidates, tierFilter, sortOrder, onTierFilterChange, onSortOrderChange }: ResultsToolbarProps) {
  const counts = countByTier(candidates)
  const presentTiers = matchTierOrder.filter((tier) => counts.has(tier))
  const isAiRankingAvailable = hasAiRanking(candidates)

  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between' }}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }} role="group" aria-label="Filter by match tier">
        <FilterListIcon fontSize="small" color="action" />
        <Chip
          size="small"
          label={`All · ${candidates.length}`}
          color={tierFilter === 'all' ? 'primary' : 'default'}
          variant={tierFilter === 'all' ? 'filled' : 'outlined'}
          onClick={() => onTierFilterChange('all')}
          aria-pressed={tierFilter === 'all'}
        />
        {presentTiers.map((tier) => {
          const presentation = getMatchTierPresentation(tier)
          const isSelected = tierFilter === tier
          return (
            <Chip
              key={tier}
              size="small"
              label={`${presentation.label} · ${counts.get(tier)}`}
              color={presentation.color}
              variant={isSelected ? 'filled' : 'outlined'}
              onClick={() => onTierFilterChange(isSelected ? 'all' : tier)}
              aria-pressed={isSelected}
            />
          )
        })}
      </Stack>

      <TextField
        select
        size="small"
        label="Sort"
        value={sortOrder}
        onChange={(event) => onSortOrderChange(event.target.value as SortOrder)}
        sx={{ minWidth: 180, flexShrink: 0, bgcolor: 'background.paper' }}
      >
        {(Object.keys(sortOrderLabels) as SortOrder[]).map((order) => (
          <MenuItem key={order} value={order} disabled={order === 'aiRanking' && !isAiRankingAvailable}>
            {sortOrderLabels[order]}
          </MenuItem>
        ))}
      </TextField>
    </Stack>
  )
}
