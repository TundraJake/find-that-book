import { useEffect, useState } from 'react'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import HourglassTopOutlinedIcon from '@mui/icons-material/HourglassTopOutlined'

const countdownTickMilliseconds = 1000

interface BusyNoticeProps {
  retryAfterSeconds: number
  onRetry: () => void
}

/** One-line notice for Gemini's "high demand" (503): counts down, then offers a retry of the same query. */
export function BusyNotice({ retryAfterSeconds, onRetry }: BusyNoticeProps) {
  const [secondsLeft, setSecondsLeft] = useState(retryAfterSeconds)

  useEffect(() => {
    if (secondsLeft <= 0) return
    const timer = window.setTimeout(() => setSecondsLeft((seconds) => seconds - 1), countdownTickMilliseconds)
    return () => window.clearTimeout(timer)
  }, [secondsLeft])

  return (
    <Alert
      severity="info"
      variant="outlined"
      icon={<HourglassTopOutlinedIcon />}
      sx={{ bgcolor: 'background.paper', alignItems: 'center' }}
      action={
        <Button size="small" variant="outlined" onClick={onRetry} disabled={secondsLeft > 0}>
          {secondsLeft > 0 ? `Try again in ${secondsLeft}s` : 'Try again'}
        </Button>
      }
    >
      Gemini is in high demand right now. Please try again in {retryAfterSeconds} seconds.
    </Alert>
  )
}
