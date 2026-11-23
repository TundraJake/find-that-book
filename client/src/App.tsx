import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Container from '@mui/material/Container'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useRef, useState } from 'react'
import { BookSearchError, searchBooks, type QuotaExhaustion } from './api/bookSearchClient.ts'
import { QuotaLimitNotice } from './components/QuotaLimitNotice.tsx'
import { RejectedSearchNotice } from './components/RejectedSearchNotice.tsx'
import { BusyNotice } from './components/BusyNotice.tsx'
import { AuthorLinkCard } from './components/AuthorLinkCard.tsx'
import { CatalogUnavailableNotice } from './components/CatalogUnavailableNotice.tsx'
import { AppHeader } from './components/AppHeader.tsx'
import { BookCard } from './components/BookCard.tsx'
import { EmptyState } from './components/EmptyState.tsx'
import { HypothesisSummary } from './components/HypothesisSummary.tsx'
import { NoResults } from './components/NoResults.tsx'
import { JsonInspector, type InspectorMode, type RequestTiming } from './components/JsonInspector.tsx'
import { PanelResizeHandle } from './components/PanelResizeHandle.tsx'
import { arrangeCandidates, type SortOrder, type TierFilter } from './components/resultOrdering.ts'
import { ResultsToolbar } from './components/ResultsToolbar.tsx'
import { ResultsSkeleton } from './components/ResultsSkeleton.tsx'
import { SearchPanel, type SearchUnavailableNotice } from './components/SearchPanel.tsx'
import { exampleQueries } from './data/exampleQueries.ts'
import { useAiUsage } from './hooks/useAiUsage.ts'
import { useResizablePanelWidth } from './hooks/useResizablePanelWidth.ts'
import { useSystemStatus, type SystemReadiness } from './hooks/useSystemStatus.ts'
import type { SearchResult } from './types.ts'

/** Matches the MUI Toolbar height on desktop; the inspector sticks just below the header. */
const headerHeightPixels = 64
const inspectorSizing = {
  defaultWidth: 960,
  minimumWidth: 320,
  maximumWidth: 960,
  minimumSiblingWidth: 560,
  keyboardStep: 24,
  storageKey: 'findThatBook.inspectorWidth',
}

const userSecretsCommand = 'dotnet user-secrets set "Gemini:ApiKey" "<key>" --project src/FindThatBook.Api'

function buildUnavailableNotice(readiness: SystemReadiness, onRetry: () => void): SearchUnavailableNotice | null {
  switch (readiness.state) {
    case 'notConfigured':
      return {
        title: 'The app is not configured correctly',
        details: [...readiness.problems, 'Set the key, then restart the API:'],
        command: userSecretsCommand,
        onRetry,
      }
    case 'unreachable':
      return { title: 'Cannot reach the API', details: ['Is the .NET backend running on port 5080?'], onRetry }
    case 'checking':
    case 'ready':
      return null
  }
}

/** True when some result is an exact title + primary author match, i.e. we found the book itself, not just its author. */
function hasExactTitleMatch(result: SearchResult): boolean {
  return result.candidates.some((candidate) => candidate.match_tier === 'exact_title_primary_author')
}

function App() {
  const inspectorPanel = useResizablePanelWidth(inspectorSizing)
  const { readiness, recheck } = useSystemStatus()
  const unavailableNotice = buildUnavailableNotice(readiness, recheck)
  const [query, setQuery] = useState('')
  const [isSearching, setIsSearching] = useState(false)
  const [result, setResult] = useState<SearchResult | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [quotaExhaustion, setQuotaExhaustion] = useState<QuotaExhaustion | null>(null)
  const [wasRejected, setWasRejected] = useState(false)
  const [busyRetryAfterSeconds, setBusyRetryAfterSeconds] = useState<number | null>(null)
  const [isCatalogUnavailable, setIsCatalogUnavailable] = useState(false)
  const lastSubmittedQuery = useRef('')
  const [inspectedIndex, setInspectedIndex] = useState<number | null>(null)
  const [inspectorMode, setInspectorMode] = useState<InspectorMode>('response')
  const [timing, setTiming] = useState<RequestTiming | null>(null)
  const [tierFilter, setTierFilter] = useState<TierFilter>('all')
  const [sortOrder, setSortOrder] = useState<SortOrder>('ranking')
  const [wasCancelled, setWasCancelled] = useState(false)
  const [usageRefreshKey, setUsageRefreshKey] = useState(0)
  const aiUsage = useAiUsage(usageRefreshKey)
  const inFlightRequest = useRef<AbortController | null>(null)

  const inspectedCandidate = result !== null && inspectedIndex !== null ? (result.candidates[inspectedIndex] ?? null) : null

  const visibleCandidates = result === null ? [] : arrangeCandidates(result.candidates, tierFilter, sortOrder)

  function inspectCandidate(index: number) {
    setInspectedIndex(index)
    setInspectorMode('candidate')
  }

  async function runSearch(submittedQuery: string) {
    inFlightRequest.current?.abort()
    const controller = new AbortController()
    inFlightRequest.current = controller
    lastSubmittedQuery.current = submittedQuery

    setIsSearching(true)
    setErrorMessage(null)
    setQuotaExhaustion(null)
    setWasRejected(false)
    setBusyRetryAfterSeconds(null)
    setIsCatalogUnavailable(false)
    setWasCancelled(false)
    setInspectedIndex(null)
    setInspectorMode('response')
    setTierFilter('all')
    setSortOrder('ranking')
    const startedAt = performance.now()
    try {
      setResult(await searchBooks(submittedQuery, controller.signal))
      setTiming({ status: 200, durationMilliseconds: Math.round(performance.now() - startedAt) })
    } catch (error) {
      if (controller.signal.aborted) return
      setResult(null)
      setTiming(null)
      setErrorMessage(error instanceof BookSearchError ? error.message : 'Something went wrong. Please try again.')
      setQuotaExhaustion(error instanceof BookSearchError ? error.quotaExhaustion : null)
      setWasRejected(error instanceof BookSearchError && error.isRejected)
      setBusyRetryAfterSeconds(error instanceof BookSearchError ? error.busyRetryAfterSeconds : null)
      setIsCatalogUnavailable(error instanceof BookSearchError && error.isCatalogUnavailable)
    } finally {
      if (inFlightRequest.current === controller) setIsSearching(false)
      setUsageRefreshKey((key) => key + 1)
    }
  }

  /** Aborts the in-flight search; the API sees the disconnect and stops its Gemini and Open Library calls. */
  function cancelSearch() {
    inFlightRequest.current?.abort()
    inFlightRequest.current = null
    setIsSearching(false)
    setResult(null)
    setTiming(null)
    setWasCancelled(true)
    setUsageRefreshKey((key) => key + 1)
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppHeader usage={aiUsage} />

      <Box sx={{ display: 'flex', flexDirection: { xs: 'column', lg: 'row' }, flexGrow: 1, minHeight: 0 }}>
        <Box component="main" sx={{ flexGrow: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
          <Box
            component="section"
            sx={{
              background: 'linear-gradient(180deg, #14263F 0%, #1F3A5F 100%)',
              color: 'primary.contrastText',
              pt: { xs: 6, md: 9 },
              pb: { xs: 12, md: 14 },
            }}
          >
            <Container maxWidth="md">
              <Typography variant="overline" sx={{ color: 'secondary.light', opacity: 0.9 }}>
                Library discovery
              </Typography>
              <Typography variant="h1" sx={{ mt: 1, fontSize: { xs: '2rem', md: '2.75rem' } }}>
                Find the book you’re thinking of.
              </Typography>
              <Typography sx={{ mt: 2, maxWidth: 560, opacity: 0.8, fontSize: '1.05rem' }}>
                Messy input welcome. AI reads your query, Open Library supplies the catalog, and every result explains why it matched.
              </Typography>
            </Container>
          </Box>

          <Container maxWidth="md" sx={{ mt: { xs: -9, md: -10 }, pb: 8, flexGrow: 1 }}>
            <SearchPanel
              query={query}
              isSearching={isSearching}
              unavailableNotice={unavailableNotice}
              exampleQueries={exampleQueries}
              onQueryChange={setQuery}
              onSearch={runSearch}
              onCancel={cancelSearch}
            />

            <Box sx={{ mt: 4 }}>
              {isSearching && <ResultsSkeleton />}

              {!isSearching && quotaExhaustion !== null && <QuotaLimitNotice quotaExhaustion={quotaExhaustion} />}

              {!isSearching && wasRejected && <RejectedSearchNotice />}

              {!isSearching && busyRetryAfterSeconds !== null && (
                <BusyNotice retryAfterSeconds={busyRetryAfterSeconds} onRetry={() => void runSearch(lastSubmittedQuery.current)} />
              )}

              {!isSearching && isCatalogUnavailable && <CatalogUnavailableNotice onRetry={() => void runSearch(lastSubmittedQuery.current)} />}

              {!isSearching && errorMessage !== null && quotaExhaustion === null && !wasRejected && busyRetryAfterSeconds === null && !isCatalogUnavailable && (
                <Alert severity="error" variant="outlined" sx={{ bgcolor: 'background.paper' }}>
                  {errorMessage}
                </Alert>
              )}

              {!isSearching && wasCancelled && (
                <Alert severity="info" variant="outlined" sx={{ bgcolor: 'background.paper' }}>
                  Search cancelled. Edit your query and search again.
                </Alert>
              )}

              {!isSearching && !wasCancelled && errorMessage === null && result === null && <EmptyState />}

              {!isSearching && result !== null && result.candidates.length === 0 && (
                <Stack spacing={2.5}>
                  <HypothesisSummary hypothesis={result.hypothesis} />
                  <NoResults query={result.query} />
                </Stack>
              )}

              {!isSearching && result !== null && result.candidates.length > 0 && (
                <Stack spacing={2.5}>
                  <HypothesisSummary hypothesis={result.hypothesis} />
                  {result.matched_author !== null && !hasExactTitleMatch(result) && (
                    <AuthorLinkCard author={result.matched_author} queriedTitle={result.hypothesis.title} />
                  )}
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
                    <Typography variant="h2" component="h2">
                      Top matches
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {visibleCandidates.length === result.candidates.length
                        ? `${result.candidates.length} results`
                        : `Showing ${visibleCandidates.length} of ${result.candidates.length}`}{' '}
                      for “{result.query}”
                    </Typography>
                  </Stack>
                  <ResultsToolbar
                    candidates={result.candidates}
                    tierFilter={tierFilter}
                    sortOrder={sortOrder}
                    onTierFilterChange={setTierFilter}
                    onSortOrderChange={setSortOrder}
                  />
                  <Stack spacing={2}>
                    {visibleCandidates.map(({ candidate, rank }) => (
                      <BookCard
                        key={candidate.work_key}
                        candidate={candidate}
                        rank={rank}
                        isInspected={inspectorMode === 'candidate' && inspectedIndex === rank - 1}
                        onInspect={() => inspectCandidate(rank - 1)}
                      />
                    ))}
                  </Stack>
                </Stack>
              )}
            </Box>
          </Container>

          <Box component="footer" sx={{ borderTop: 1, borderColor: 'divider', py: 3 }}>
            <Container maxWidth="md">
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                Book data from Open Library
              </Typography>
            </Container>
          </Box>
        </Box>

        <Box
          component="aside"
          aria-label="JSON inspector"
          sx={{
            width: { xs: '100%', lg: inspectorPanel.width },
            flexShrink: 0,
            borderLeft: { lg: 1 },
            borderTop: { xs: 1, lg: 0 },
            borderColor: 'divider',
            position: { lg: 'sticky' },
            top: { lg: headerHeightPixels },
            height: { lg: `calc(100vh - ${headerHeightPixels}px)` },
          }}
        >
          <PanelResizeHandle isDragging={inspectorPanel.isDragging} {...inspectorPanel.handleProps} />
          <JsonInspector
            result={result}
            selectedCandidate={inspectedCandidate}
            selectedRank={inspectedIndex === null ? null : inspectedIndex + 1}
            mode={inspectorMode}
            timing={timing}
            onModeChange={setInspectorMode}
          />
        </Box>
      </Box>
    </Box>
  )
}

export default App
