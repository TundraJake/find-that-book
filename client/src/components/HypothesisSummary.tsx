import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { QueryHypothesis } from '../types.ts'

interface HypothesisSummaryProps {
  hypothesis: QueryHypothesis
}

export function HypothesisSummary({ hypothesis }: HypothesisSummaryProps) {
  return (
    <Paper sx={{ px: 2.5, py: 2, bgcolor: 'rgba(31, 58, 95, 0.04)', border: 1, borderColor: 'rgba(31, 58, 95, 0.12)' }}>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ alignItems: { md: 'center' } }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexShrink: 0 }}>
          <AutoAwesomeOutlinedIcon fontSize="small" color="primary" />
          <Typography variant="subtitle2" color="primary">
            What Gemini understood
          </Typography>
        </Stack>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
          {hypothesis.title && <Chip size="small" color="primary" label={`Title: ${hypothesis.title}`} />}
          {hypothesis.author && <Chip size="small" color="primary" label={`Author: ${hypothesis.author}`} />}
          {hypothesis.year_hint && <Chip size="small" color="primary" variant="outlined" label={`Year: ${hypothesis.year_hint}`} />}
          {hypothesis.character_hints.map((character) => (
            <Chip key={`character-${character}`} size="small" color="secondary" variant="outlined" label={`Character: ${character}`} />
          ))}
          {hypothesis.keywords.map((keyword) => (
            <Chip key={keyword} size="small" variant="outlined" label={keyword} />
          ))}
        </Stack>
      </Stack>
    </Paper>
  )
}
