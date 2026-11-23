import type { ReactNode } from 'react'

const tokenPattern = /("(?:\\u[a-fA-F0-9]{4}|\\[^u]|[^\\"])*")(\s*:)?|\b(true|false|null)\b|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)/g

export const jsonColors = {
  key: '#9CDCFE',
  string: '#CE9178',
  number: '#B5CEA8',
  literal: '#569CD6',
  punctuation: '#D4D4D4',
}

/** Splits pretty-printed JSON into colored spans (keys, strings, numbers, true/false/null). */
export function highlightJson(json: string): ReactNode[] {
  const nodes: ReactNode[] = []
  let lastIndex = 0

  for (const match of json.matchAll(tokenPattern)) {
    const index = match.index ?? 0
    if (index > lastIndex) nodes.push(json.slice(lastIndex, index))

    const [whole, quoted, colon, literal, number] = match
    if (quoted !== undefined && colon !== undefined) {
      nodes.push(<span key={index} style={{ color: jsonColors.key }}>{quoted}</span>, colon)
    } else if (quoted !== undefined) {
      nodes.push(<span key={index} style={{ color: jsonColors.string }}>{quoted}</span>)
    } else if (literal !== undefined) {
      nodes.push(<span key={index} style={{ color: jsonColors.literal }}>{literal}</span>)
    } else if (number !== undefined) {
      nodes.push(<span key={index} style={{ color: jsonColors.number }}>{number}</span>)
    } else {
      nodes.push(whole)
    }
    lastIndex = index + whole.length
  }

  if (lastIndex < json.length) nodes.push(json.slice(lastIndex))
  return nodes
}
