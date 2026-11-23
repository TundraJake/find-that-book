# Open Library — endpoints used and data quirks

Observed against openlibrary.org on 2026-11-22. Fixtures: `tests/FindThatBook.UnitTests/Fixtures/OpenLibrary/`.

## Endpoints
| Call | Used for |
|---|---|
| `GET /search.json?title=…&author=…` | Title + author candidates |
| `GET /search.json?title=…` | Title-only candidates |
| `GET /search.json?author=…&sort=editions` | "Top works by that author" (author-only fallback), most editions first |
| `GET /search.json?q=…` | Character hints / nothing structured extracted |
| `GET /works/{id}.json` | Canonical `authors` (primary vs contributor), title, subtitle, covers |
| `GET /authors/{id}.json` | Author name when search results did not include it |
| `GET /authors/{id}/works.json?limit=1000` | Works the queried author is credited on (canonical `authors`). Those results are enriched first, ahead of works where the author is only mentioned. Never drops a result; a failure here only keeps the search order. |

Every search sends `fields=key,title,subtitle,author_name,author_key,first_publish_year,cover_i,edition_count` and `limit`.
Requests carry the `User-Agent` from `OpenLibrary:UserAgent` (Open Library asks for an app name and contact).

## When Open Library is slow
- Open Library can take well over the 15-second per-request limit (`OpenLibrary:TimeoutSeconds`) at busy times.
- One of the parallel `/search.json` calls that fails or times out is skipped and the others are used; the search fails only if all of them fail.
- A work lookup that fails or times out drops just that book; the search fails only if every lookup fails.
- `/authors/{id}/works.json` (up to 1,000 entries) is fetched only when there are more results than `EnrichLimit`, because only then can it change which works are enriched. Its failure never fails a search.

## Quirks the code handles
- **`/authors/{id}/works.json` is newest record first, not by popularity.** Tolkien has 367 entries, and the first ones at capture time were Finnish translations added that week. So it confirms authorship and is not used to pick "top works" (that is the `sort=editions` search). The author key comes from `author_key` in search results, matched by surname.
- **Merged works redirect.** `/works/OL262758W.json` returns `{"type":{"key":"/type/redirect"},"location":"/works/OL27482W"}`. The client follows up to 3 hops.
- **`author_name` mixes in contributors.** The graphic-novel Hobbit (`OL219602W`) lists Charles Dixon, Sean Deming, and J.R.R. Tolkien.
- **Work records do not always separate roles either.** `OL219602W`'s canonical `authors` also lists all three, with no `role`. The ranker therefore breaks ties by the queried author's position in the list, then by edition count, so the canonical Tolkien work (482 editions) ranks above the adaptation (10).
- **References come in two shapes.** Newer records write `"type": {"key": "/type/author_role"}`; older ones write `"type": "/type/author_role"`. `KeyReferenceJsonConverter` accepts both, and a work record that still cannot be parsed is skipped (logged) instead of failing the search.
- **Duplicate works.** Two "Pride and Prejudice" works exist (4042 vs 132 editions); candidates are de-duplicated by normalized title + first author, keeping the one with most editions.
- **Noisy fields.** `first_publish_year` can be wrong (A Tale of Two Cities shows 1800); `covers` can contain `-1`. Negative cover ids are ignored.
- **`/authors/{id}/works.json` is unordered** (mostly translations), so author-only results use `search.json?author=…&sort=editions` instead.
