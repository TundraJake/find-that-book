import CheckIcon from '@mui/icons-material/Check'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import DataObjectIcon from '@mui/icons-material/DataObject'
import Box from '@mui/material/Box'
import Chip from '@mui/material/Chip'
import IconButton from '@mui/material/IconButton'
import Stack from '@mui/material/Stack'
import ToggleButton from '@mui/material/ToggleButton'
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup'
import Tooltip from '@mui/material/Tooltip'
import Typography from '@mui/material/Typography'
import { useMemo, useState } from 'react'
import type { BookCandidate, SearchResult } from '../types.ts'
import { jsonColors } from './highlightJson.tsx'
import { JsonLines } from './JsonLines.tsx'

export type InspectorMode = 'response' | 'candidate'

export interface RequestTiming {
  status: number
  durationMilliseconds: number
}

interface JsonInspectorProps {
  result: SearchResult | null
  selectedCandidate: BookCandidate | null
  selectedRank: number | null
  mode: InspectorMode
  timing: RequestTiming | null
  onModeChange: (mode: InspectorMode) => void
}

const copiedResetMilliseconds = 1500

export function JsonInspector({ result, selectedCandidate, selectedRank, mode, timing, onModeChange }: JsonInspectorProps) {
  const [hasCopied, setHasCopied] = useState(false)

  const inspectedValue: unknown = mode === 'candidate' ? selectedCandidate : result
  const json = useMemo(() => (inspectedValue === null ? '' : JSON.stringify(inspectedValue, null, 2)), [inspectedValue])

  async function copyJson() {
    await navigator.clipboard.writeText(json)
    setHasCopied(true)
    window.setTimeout(() => setHasCopied(false), copiedResetMilliseconds)
  }

  function handleModeChange(_: unknown, nextMode: InspectorMode | null) {
    if (nextMode !== null) onModeChange(nextMode)
  }

  const subtitle =
    mode === 'candidate' ? (selectedRank === null ? 'Click a result card to inspect it' : `candidates[${selectedRank - 1}]`) : 'POST /api/books/search'

  return (
    <Stack sx={{ height: '100%', minHeight: 0, bgcolor: 'background.paper' }}>
      <Stack spacing={1.5} sx={{ px: 2.5, pt: 2.5, pb: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <DataObjectIcon color="primary" fontSize="small" />
          <Typography variant="subtitle1" sx={{ fontWeight: 650, flexGrow: 1 }}>
            JSON inspector
          </Typography>
          <Tooltip title={hasCopied ? 'Copied' : 'Copy JSON'}>
            <span>
              <IconButton size="small" aria-label="Copy JSON" onClick={copyJson} disabled={json.length === 0}>
                {hasCopied ? <CheckIcon fontSize="small" color="success" /> : <ContentCopyIcon fontSize="small" />}
              </IconButton>
            </span>
          </Tooltip>
        </Stack>

        <ToggleButtonGroup size="small" exclusive fullWidth value={mode} onChange={handleModeChange} aria-label="Inspector view">
          <ToggleButton value="response">Full response</ToggleButton>
          <ToggleButton value="candidate" disabled={result === null}>
            Selected card
          </ToggleButton>
        </ToggleButtonGroup>

        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minHeight: 24 }}>
          <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'ui-monospace, Consolas, monospace', flexGrow: 1 }}>
            {subtitle}
          </Typography>
          {timing && mode === 'response' && (
            <>
              <Chip size="small" color={timing.status < 400 ? 'success' : 'error'} variant="outlined" label={timing.status} />
              <Chip size="small" variant="outlined" label={`${timing.durationMilliseconds} ms`} />
            </>
          )}
        </Stack>
      </Stack>

      <Box
        role="region"
        aria-label="JSON"
        sx={{
          m: 0,
          p: 2.5,
          flexGrow: 1,
          minHeight: 240,
          overflow: 'auto',
          bgcolor: '#0F1B2D',
          color: jsonColors.punctuation,
          fontFamily: 'ui-monospace, "Cascadia Code", Consolas, "SF Mono", monospace',
          fontSize: 12.5,
          lineHeight: 1.6,
        }}
      >
        {json.length > 0 ? (
          <JsonLines json={json} />
        ) : (
          <span style={{ color: '#6B7A90' }}>{result === null ? '// Run a search to see the API response' : '// Select a result card to inspect it'}</span>
        )}
      </Box>
    </Stack>
  )
}
