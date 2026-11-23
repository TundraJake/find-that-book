# Gemini `GenerateContentResponse` — reference template

Shape of the response returned by `POST {BaseUrl}/{ApiVersion}/models/{Model}:generateContent`.
Source: Google's API reference, <https://ai.google.dev/api/generate-content> (captured 2026-11-22).

`←` marks the fields this app reads (`Infrastructure/Gemini/Models/GenerateContentResponse.cs`). Everything else is ignored on deserialization.

```jsonc
{
  "candidates": [                                   // ← we read candidates[0]
    {
      "content": {                                  // Content
        "role": "model",
        "parts": [                                  // Part[]
          {
            "text": "string",                       // ← the model's JSON output, as a string
            "thought": false,                       //   true = model "thinking" text, not the answer
            "thoughtSignature": "string",
            "partMetadata": {},
            "mediaResolution": {},
            "mediaProcessing": {},
            "audioTranscription": {},
            "speechMetadata": {}
            // data union (exactly one of): text | inlineData | functionCall | functionResponse |
            //   fileData | executableCode | codeExecutionResult | toolCall | toolResponse
            // metadata union: videoMetadata
          }
        ]
      },
      "finishReason": "STOP",                       // ← must be STOP (values below)
      "safetyRatings": [ { "category": "HARM_CATEGORY_…", "probability": "NEGLIGIBLE", "blocked": false } ],
      "citationMetadata": {},
      "tokenCount": 0,
      "groundingAttributions": [],
      "groundingMetadata": {},
      "avgLogprobs": 0.0,
      "logprobsResult": {},
      "urlContextMetadata": {},
      "continuationToken": "string",
      "index": 0,
      "finishMessage": "string"
    }
  ],
  "promptFeedback": {                               // present only when the prompt was rejected
    "blockReason": "SAFETY",                        // ← any value = reject
    "safetyRatings": [ { "category": "…", "probability": "…", "blocked": true } ]
  },
  "usageMetadata": {
    "promptTokenCount": 0,
    "cachedContentTokenCount": 0,
    "candidatesTokenCount": 0,
    "toolUsePromptTokenCount": 0,
    "thoughtsTokenCount": 0,
    "totalTokenCount": 0,
    "promptTokensDetails":        [ { "modality": "TEXT", "tokenCount": 0 } ],
    "cacheTokensDetails":         [ { "modality": "TEXT", "tokenCount": 0 } ],
    "candidatesTokensDetails":    [ { "modality": "TEXT", "tokenCount": 0 } ],
    "toolUsePromptTokensDetails": [ { "modality": "TEXT", "tokenCount": 0 } ],
    "serviceTier": "…"
  },
  "modelVersion": "string",
  "responseId": "string",
  "modelStatus": { "modelStage": "…", "retirementTime": "string", "message": "string" }
}
```

## Enums

**`finishReason`** — we accept only `STOP`:
`STOP`, `MAX_TOKENS`, `SAFETY`, `RECITATION`, `LANGUAGE`, `OTHER`, `BLOCKLIST`, `PROHIBITED_CONTENT`, `SPII`,
`MALFORMED_FUNCTION_CALL`, `IMAGE_SAFETY`, `IMAGE_PROHIBITED_CONTENT`, `IMAGE_OTHER`, `NO_IMAGE`, `IMAGE_RECITATION`,
`UNEXPECTED_TOOL_CALL`, `TOO_MANY_TOOL_CALLS`, `MISSING_THOUGHT_SIGNATURE`, `MALFORMED_RESPONSE`, `ESCALATION`,
`PUP_LIMITED_DISABLED`, `CONTINUATION`, `FINISH_REASON_UNSPECIFIED`.

**`promptFeedback.blockReason`** — any value means the prompt was rejected:
`BLOCK_REASON_UNSPECIFIED`, `SAFETY`, `OTHER`, `BLOCKLIST`, `PROHIBITED_CONTENT`, `IMAGE_SAFETY`.

## How this app validates it (`GeminiClient.ReadJsonText`)
1. Non-2xx HTTP status → reject.
2. Empty body → reject.
3. `promptFeedback.blockReason` present → reject.
4. `candidates` empty → reject.
5. `finishReason` ≠ `STOP` → reject (e.g. `MAX_TOKENS` means truncated JSON).
6. Joined `parts[].text` empty → reject.

A rejection throws `GeminiException`; `GeminiQueryExtractor` logs it and rethrows it as `QueryExtractionFailedException`, which the API returns as a 502 ProblemDetails. There is no non-AI fallback.

## Example (successful extraction)
```json
{
  "candidates": [{
    "content": {
      "role": "model",
      "parts": [{ "text": "{\"title\":\"The Hobbit\",\"author\":\"J.R.R. Tolkien\",\"keywords\":[\"illustrated\",\"deluxe\"],\"characterHints\":[],\"yearHint\":1937}" }]
    },
    "finishReason": "STOP"
  }],
  "usageMetadata": { "promptTokenCount": 180, "candidatesTokenCount": 40, "totalTokenCount": 220 }
}
```
