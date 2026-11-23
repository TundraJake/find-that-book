import Alert from '@mui/material/Alert'
import AlertTitle from '@mui/material/AlertTitle'
import Typography from '@mui/material/Typography'
import BedtimeOutlinedIcon from '@mui/icons-material/BedtimeOutlined'
import HourglassTopOutlinedIcon from '@mui/icons-material/HourglassTopOutlined'
import type { QuotaExhaustion } from '../api/bookSearchClient.ts'

const secondsPerMinute = 60
const secondsPerHour = 3600

/** "about 9 hours", "about 12 minutes", "a few seconds": rough on purpose, the reset time is approximate. */
function describeWait(seconds: number): string {
  if (seconds >= secondsPerHour) {
    const hours = Math.round(seconds / secondsPerHour)
    return `about ${hours} hour${hours === 1 ? '' : 's'}`
  }
  if (seconds >= secondsPerMinute) {
    const minutes = Math.round(seconds / secondsPerMinute)
    return `about ${minutes} minute${minutes === 1 ? '' : 's'}`
  }
  return seconds > 1 ? `about ${seconds} seconds` : 'a few seconds'
}

interface QuotaLimitNoticeProps {
  quotaExhaustion: QuotaExhaustion
}

/** Friendly notice shown when every Gemini model is over quota, instead of a red error. */
export function QuotaLimitNotice({ quotaExhaustion }: QuotaLimitNoticeProps) {
  const wait = describeWait(quotaExhaustion.retryAfterSeconds)

  if (quotaExhaustion.scope === 'daily') {
    return (
      <Alert severity="warning" variant="outlined" icon={<BedtimeOutlinedIcon />} sx={{ bgcolor: 'background.paper', py: 2 }}>
        <AlertTitle sx={{ fontWeight: 600 }}>Daily usage limits reached</AlertTitle>
        <Typography variant="body2" sx={{ mb: 1 }}>
          Find That Book has used today&apos;s Gemini allowance on every model, so it can&apos;t answer new searches right now.
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Please come back tomorrow. Limits reset at midnight Pacific time, in {wait}.
        </Typography>
      </Alert>
    )
  }

  return (
    <Alert severity="info" variant="outlined" icon={<HourglassTopOutlinedIcon />} sx={{ bgcolor: 'background.paper', py: 2 }}>
      <AlertTitle sx={{ fontWeight: 600 }}>Gemini is catching its breath</AlertTitle>
      <Typography variant="body2">
        Every model is at its per-minute request limit. Please try again in {wait}.
      </Typography>
    </Alert>
  )
}
