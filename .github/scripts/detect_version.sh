# Determines the next semantic-release version for a project.
#
# Usage: detect_version <ProjectName> <ProjectPath>
#
# Outputs "version=X.Y.Z" to $GITHUB_OUTPUT when a new version is warranted.
# Skips silently when: 
#   - A prior release tag exists AND no source files changed since that tag
#   - Commits since last tag contain no conventional release triggers
#
# Requires GITHUB_TOKEN and RELEASE_PROJECT to be set in the environment.
detect_version() {
  local project=$1
  local project_path=$2

  local last_tag
  last_tag=$(git tag -l "${project}[0-9]*" --sort=-v:refname | head -1)

  if [ -n "$last_tag" ] && [ -z "$(git diff --name-only "$last_tag" HEAD -- "$project_path")" ]; then
    echo "No changes in $project since $last_tag — skipping"
    return 0
  fi

  local sr_out version
  sr_out=$(RELEASE_PROJECT="$project" npx semantic-release --dry-run 2>&1)
  version=$(echo "$sr_out" | grep "next release version" | grep -Eo '[0-9]+\.[0-9]+\.[0-9]+' | head -1 || true)

  if [ -n "$version" ]; then
    echo "Next version for $project: $version"
    echo "version=$version" >> "$GITHUB_OUTPUT"
  else
    echo "No conventional release commits found for $project"
  fi
}
