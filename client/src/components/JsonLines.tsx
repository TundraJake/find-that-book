import Box from '@mui/material/Box'
import { useMemo } from 'react'
import { highlightJson } from './highlightJson.tsx'

/** Extra indent (in characters) for the wrapped continuation of a long line, so it reads as part of the line above. */
const continuationIndentCharacters = 2

interface JsonLine {
  indentCharacters: number
  content: string
}

function splitIntoLines(json: string): JsonLine[] {
  return json.split('\n').map((line) => {
    const content = line.trimStart()
    return { indentCharacters: line.length - content.length, content }
  })
}

interface JsonLinesProps {
  json: string
}

/**
 * Renders pretty-printed JSON one line per block. Long lines wrap with a hanging indent,
 * so wrapped text stays aligned under its own indentation level instead of returning to column 0.
 */
export function JsonLines({ json }: JsonLinesProps) {
  const lines = useMemo(() => splitIntoLines(json), [json])

  return (
    <>
      {lines.map((line, index) => (
        <Box
          key={index}
          sx={{
            pl: `${line.indentCharacters + continuationIndentCharacters}ch`,
            textIndent: `-${continuationIndentCharacters}ch`,
            whiteSpace: 'pre-wrap',
            overflowWrap: 'anywhere',
          }}
        >
          {highlightJson(line.content)}
        </Box>
      ))}
    </>
  )
}
