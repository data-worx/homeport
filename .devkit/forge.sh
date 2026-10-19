#!/usr/bin/env bash
# GitHub implementation of the forge contract. Managed by devkit-sync -
# edit it upstream in raisr/devkit, not here.
#
# Every forge pack defines the same function names, so the skills never
# mention gh or glab. Sourced, not executed.

forge_name()      { echo "github"; }
forge_pr_term()   { echo "pull request"; }
forge_pr_short()  { echo "PR"; }

# Is the CLI there and authenticated, and which repository does it point at?
forge_check() {
  gh auth status >/dev/null 2>&1 || { echo "gh is not authenticated" >&2; return 1; }
  gh repo view --json nameWithOwner -q .nameWithOwner
}

# forge_labels_list -> one label name per line
forge_labels_list() {
  gh label list --json name -q '.[].name'
}

# forge_label_create <name> <hex colour> <description>
forge_label_create() {
  gh label create "$1" --color "$2" --description "$3"
}

# forge_issue_list_open -> "<number>|<labels, comma separated>|<title>"
forge_issue_list_open() {
  gh issue list --state open --json number,title,labels \
    -q '.[] | "\(.number)|\(.labels | map(.name) | join(","))|\(.title)"'
}

# forge_issue_create <title> <label> <body file> [assignee] -> issue URL
# The assignee is left off when it is empty, so the issue stays unassigned.
forge_issue_create() {
  gh issue create --title "$1" --label "$2" --body-file "$3" \
    ${4:+--assignee "$4"}
}

# forge_issue_view <number> -> "#<n> [<state>] <title>   labels=<a,b>"
forge_issue_view() {
  gh issue view "$1" --json number,title,state,labels \
    -q '"#\(.number) [\(.state)] \(.title)   labels=\(.labels|map(.name)|join(","))"'
}

# forge_issue_reply <number> <body file> - where a finding made while working
# the ticket goes (forge.issues), rather than into the description. A file, not
# a string: the note names files and rules, so it is full of backticks.
forge_issue_reply() {
  gh issue comment "$1" --body-file "$2"
}

# forge_issue_body <number> -> raw body
forge_issue_body() {
  gh issue view "$1" --json body -q .body
}

# forge_issue_milestone <number> -> milestone title, or nothing when it has none
forge_issue_milestone() {
  gh issue view "$1" --json milestone -q '.milestone.title // empty'
}

# forge_issue_edit <number> <body file> - replaces the whole body. For a ticket
# that was wrong when it was filed, before work starts, and for ticking its
# Definition of Done after the merge (forge.review); a finding made while
# working it goes into forge_issue_reply instead (forge.issues).
forge_issue_edit() {
  gh issue edit "$1" --body-file "$2"
}

# forge_pr_create <base> <head> <title> <label> <assignee> <body file>
#   [milestone] -> URL
# The milestone is left off when it is empty, so a request whose issue has
# none is created without one.
forge_pr_create() {
  gh pr create --base "$1" --head "$2" --title "$3" \
    --label "$4" --assignee "$5" --body-file "$6" ${7:+--milestone "$7"}
}

# forge_pr_view <branch or number> -> "#<n> state=<s> draft=<b> review=<r>\n<url>"
forge_pr_view() {
  gh pr view "$1" --json number,state,isDraft,reviewDecision,url \
    -q '"#\(.number) state=\(.state) draft=\(.isDraft) review=\(.reviewDecision)\n\(.url)"'
}

# forge_pr_comments <number> -> review and issue comments, one block each
forge_pr_comments() {
  gh pr view "$1" --json comments,reviews \
    -q '.comments[].body, (.reviews[] | "\(.state): \(.body)")'
}

# forge_pr_line_comments <number> -> "<path>:<line>  <body>"
forge_pr_line_comments() {
  local repo
  repo="$(gh repo view --json nameWithOwner -q .nameWithOwner)"
  gh api "repos/${repo}/pulls/$1/comments" -q '.[] | "\(.path):\(.line)  \(.body)"'
}

# forge_pr_assign <number> <assignee>
forge_pr_assign() {
  gh pr edit "$1" --add-assignee "$2"
}

# forge_pr_edit <number> <body file> - replaces the whole body
forge_pr_edit() {
  gh pr edit "$1" --body-file "$2"
}

# forge_pr_reply <number> <body file> - a comment on the request itself, which
# is what forge.review asks a review round to end with. A file, not a string:
# the reply names commits and rules, so it is full of backticks.
forge_pr_reply() {
  gh pr comment "$1" --body-file "$2"
}
