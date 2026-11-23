/** A clickable example search and what it demonstrates (doubles as a manual test checklist). */
export interface ExampleQuery {
  query: string
  purpose: string
}

export const exampleQueries: ExampleQuery[] = [
  {
    query: 'tolkien hobbit illustrated deluxe 1937',
    purpose: 'Noisy query: title, author, edition words and a year. Should rank Tolkien’s original above the graphic-novel adaptation.',
  },
  {
    query: 'mark huckleberry',
    purpose: 'Ambiguous: a first name plus a character. Should resolve to Twain’s Adventures of Huckleberry Finn.',
  },
  {
    query: 'dickens tale two cities',
    purpose: 'Sparse title with missing words ("A", "of"). Tests title normalization.',
  },
  {
    query: 'austen bennet',
    purpose: 'Character name only, no title. Falls back to Austen’s top works; compare Best match with AI ranking.',
  },
  {
    query: 'twilight meyer',
    purpose: 'Short title plus surname only. Should pick Twilight over the rest of the series.',
  },
  {
    query: 'HACKED Jon Erickson',
    purpose: 'Wrong word form of the title (Hacking) with the full author name. Also a word the safety checks must not block.',
  },
]
