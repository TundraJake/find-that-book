import SpeedOutlinedIcon from '@mui/icons-material/SpeedOutlined'
import Chip from '@mui/material/Chip'
import Tooltip from '@mui/material/Tooltip'
import type { AiUsage } from '../api/usageClient.ts'

/** Share of the per-minute limit at which the indicator turns amber. */
const nearLimitRatio = 0.8

interface AiUsageIndicatorProps {
  usage: AiUsage | null
}

/** Development aid: Gemini requests in the last minute versus the configured limit. */
export function AiUsageIndicator({ usage }: AiUsageIndicatorProps) {
  if (usage === null) return null

  const { requests_in_window: requestsInWindow, requests_per_minute_limit: requestsPerMinuteLimit, window_seconds: windowSeconds } = usage
  const color = requestsInWindow >= requestsPerMinuteLimit ? 'error' : requestsInWindow >= requestsPerMinuteLimit * nearLimitRatio ? 'warning' : 'default'

  return (
    <Tooltip
      title={`Gemini requests this API made in the last ${windowSeconds} seconds. Each search uses 2. The limit comes from Gemini:RequestsPerMinuteLimit in appsettings.json.`}
    >
      <Chip
        icon={<SpeedOutlinedIcon />}
        size="small"
        variant="outlined"
        color={color}
        label={`Gemini ${requestsInWindow} / ${requestsPerMinuteLimit} this minute`}
        sx={{ fontVariantNumeric: 'tabular-nums' }}
      />
    </Tooltip>
  )
}
