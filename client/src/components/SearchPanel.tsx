import ClearIcon from '@mui/icons-material/Clear'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import AlertTitle from '@mui/material/AlertTitle'
import SearchIcon from '@mui/icons-material/Search'
import StopCircleOutlinedIcon from '@mui/icons-material/StopCircleOutlined'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import IconButton from '@mui/material/IconButton'
import InputAdornment from '@mui/material/InputAdornment'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import type { FormEvent, KeyboardEvent, MouseEvent } from 'react'
import ButtonBase from '@mui/material/ButtonBase'
import type { ExampleQuery } from '../data/exampleQueries.ts'

/** Mirrors BookSearchRequest.MaximumQueryLength in src/FindThatBook.Api/Contracts/BookSearchRequest.cs. */
const maximumQueryLength = 300
const visibleRows = 3
const maximumRowsBeforeScroll = 6
/** Counter turns amber once the query reaches this share of the limit. */
const nearLimitRatio = 0.9
/** Matches the height of a single-row MUI outlined input so the button lines up with the first row. */
const submitButtonHeightPixels = 56

/** Shown inside the search box when searching is not possible; the controls are disabled while it is set. */
export interface SearchUnavailableNotice {
  title: string
  details: string[]
  /** Optional shell command that fixes the problem, shown as a code block. */
  command?: string
  onRetry: () => void
}

interface SearchPanelProps {
  query: string
  isSearching: boolean
  unavailableNotice: SearchUnavailableNotice | null
  exampleQueries: ExampleQuery[]
  onQueryChange: (query: string) => void
  onSearch: (query: string) => void
  onCancel: () => void
}

export function SearchPanel({ query, isSearching, unavailableNotice, exampleQueries, onQueryChange, onSearch, onCancel }: SearchPanelProps) {
  const trimmedQuery = query.trim()
  const isUnavailable = unavailableNotice !== null

  const isAtLimit = query.length >= maximumQueryLength
  const isNearLimit = query.length >= maximumQueryLength * nearLimitRatio
  const counterColor = isAtLimit ? 'error.main' : isNearLimit ? 'warning.main' : 'text.secondary'

  function submitQuery() {
    // Submitting while a search runs replaces it (the previous request is aborted).
    if (trimmedQuery.length > 0 && !isUnavailable) onSearch(trimmedQuery)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    submitQuery()
  }

  /**
   * Separate keys keep Cancel and Find it as different DOM buttons; preventDefault stops the click
   * from finishing as a form submit after React swaps Cancel back to the submit button.
   */
  function handleCancelClick(event: MouseEvent<HTMLButtonElement>) {
    event.preventDefault()
    onCancel()
  }

  /** Enter searches (replacing a running search); Shift+Enter inserts a new line; Esc cancels a running search. */
  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault()
      submitQuery()
    } else if (event.key === 'Escape' && isSearching) {
      event.preventDefault()
      onCancel()
    }
  }

  function handleExampleClick(example: string) {
    onQueryChange(example)
    onSearch(example)
  }

  return (
    <Paper
      component="form"
      onSubmit={handleSubmit}
      sx={{ p: { xs: 2, sm: 3 }, border: 1, borderColor: 'divider', boxShadow: '0 12px 40px rgba(20, 38, 63, 0.06)' }}
    >
      {unavailableNotice !== null && (
        <Alert
          severity="warning"
          sx={{ mb: 2 }}
          action={
            <Button color="inherit" size="small" onClick={unavailableNotice.onRetry}>
              Check again
            </Button>
          }
        >
          <AlertTitle>{unavailableNotice.title}</AlertTitle>
          {unavailableNotice.details.map((detail) => (
            <div key={detail}>{detail}</div>
          ))}
          {unavailableNotice.command && (
            <Box
              component="code"
              sx={{ display: 'block', mt: 1, px: 1.25, py: 0.75, borderRadius: 1, bgcolor: 'rgba(0, 0, 0, 0.06)', fontSize: 12.5, overflowWrap: 'anywhere' }}
            >
              {unavailableNotice.command}
            </Box>
          )}
        </Alert>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ alignItems: { sm: 'flex-start' } }}>
        <TextField
          fullWidth
          autoFocus
          disabled={isUnavailable}
          multiline
          minRows={visibleRows}
          maxRows={maximumRowsBeforeScroll}
          value={query}
          onChange={(event) => onQueryChange(event.target.value.slice(0, maximumQueryLength))}
          onKeyDown={handleKeyDown}
          placeholder="Title, author, character, keywords… anything you remember"
          helperText={
            <Stack direction="row" component="span" sx={{ justifyContent: 'space-between', gap: 2 }}>
              <span>Enter to search · Shift+Enter for a new line · Esc to cancel · Answers can take up to 60 seconds</span>
              <Typography component="span" variant="caption" aria-live="polite" sx={{ color: counterColor, fontVariantNumeric: 'tabular-nums', flexShrink: 0 }}>
                {query.length} / {maximumQueryLength}
              </Typography>
            </Stack>
          }
          slotProps={{
            htmlInput: { 'aria-label': 'Book search query', maxLength: maximumQueryLength },
            formHelperText: { component: 'div', sx: { mx: 0.5 } },
            input: {
              startAdornment: (
                <InputAdornment position="start" sx={{ alignSelf: 'flex-start', mt: 1.5 }}>
                  <SearchIcon color="action" />
                </InputAdornment>
              ),
              endAdornment: query.length > 0 && (
                <InputAdornment position="end" sx={{ alignSelf: 'flex-start', mt: 1.5 }}>
                  <IconButton aria-label="Clear search" size="small" edge="end" onClick={() => onQueryChange('')}>
                    <ClearIcon fontSize="small" />
                  </IconButton>
                </InputAdornment>
              ),
              sx: { fontSize: '1.05rem', bgcolor: 'background.default', alignItems: 'flex-start' },
            },
          }}
        />
        {isSearching ? (
          <Button
            key="cancel"
            variant="outlined"
            size="large"
            color="inherit"
            onClick={handleCancelClick}
            startIcon={<StopCircleOutlinedIcon />}
            sx={{ px: 3, minWidth: 140, height: submitButtonHeightPixels, flexShrink: 0 }}
          >
            Cancel
          </Button>
        ) : (
          <Button
            key="submit"
            type="submit"
            variant="contained"
            size="large"
            disabled={trimmedQuery.length === 0 || isUnavailable}
            sx={{ px: 4, minWidth: 140, height: submitButtonHeightPixels, flexShrink: 0 }}
          >
            Find it
          </Button>
        )}
      </Stack>

      <Box sx={{ mt: 2.5 }}>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          Try one of these. Each shows a different kind of messy query:
        </Typography>
        <Stack component="ul" sx={{ listStyle: 'none', m: 0, p: 0, border: 1, borderColor: 'divider', borderRadius: 2, overflow: 'hidden' }}>
          {exampleQueries.map((example, index) => (
            <Box component="li" key={example.query} sx={{ borderTop: index === 0 ? 0 : 1, borderColor: 'divider' }}>
              <ButtonBase
                onClick={() => handleExampleClick(example.query)}
                disabled={isSearching || isUnavailable}
                sx={{
                  width: '100%',
                  justifyContent: 'flex-start',
                  textAlign: 'left',
                  px: 1.5,
                  py: 1,
                  gap: 0.5,
                  flexDirection: 'column',
                  alignItems: 'flex-start',
                  '&:hover': { bgcolor: 'action.hover' },
                  '&.Mui-disabled': { opacity: 0.5 },
                }}
              >
                <Chip label={example.query} size="small" variant="outlined" sx={{ fontFamily: 'monospace', flexShrink: 0, cursor: 'inherit' }} />
                <Typography variant="caption" color="text.secondary" sx={{ lineHeight: 1.4, pl: 0.5 }}>
                  {example.purpose}
                </Typography>
              </ButtonBase>
            </Box>
          ))}
        </Stack>
      </Box>
    </Paper>
  )
}
