workflow "Build and Test" {
  on = "push"
  resolves = ["twitchax/actions/dotnet/cli@master"]
}

action "twitchax/actions/dotnet/cli@master" {
  uses = "twitchax/actions/dotnet/cli@master"
  args = "test"
}
