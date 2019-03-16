workflow "Build and Test" {
  on = "push"
  resolves = ["twitchax/actions/dotnet/cli@master"]
}

action "twitchax/actions/dotnet/cli@master" {
  uses = "twitchax/actions/dotnet/cli@master"
  args = "test"
}

workflow "Release" {
  on = "release"
  resolves = ["nuget-push"]
}

action "pack" {
  uses = "twitchax/actions/dotnet/cli@master"
  args = "pack -c Release"
}

action "nuget-push" {
  uses = "twitchax/actions/dotnet/nuget-push@master"
  needs = ["pack"]
  args = "**/AspNetCore.Proxy.*.nupkg"
  secrets = ["NUGET_KEY"]
}
