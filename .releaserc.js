'use strict';

const project = process.env.RELEASE_PROJECT;

if (!project) {
  throw new Error('RELEASE_PROJECT environment variable is required');
}

const projects = {
  Util: {
    csproj: 'BeClean/BeClean.Util/BeClean.Util.csproj',
  },
  Repository: {
    csproj: 'BeClean/BeClean.Repository/BeClean.Repository.csproj',
  },
  Localization: {
    csproj: 'BeClean/BeClean.Localization/BeClean.Localization.csproj',
  },
  DataLayer: {
    csproj: 'BeClean/BeClean.DataLayer/BeClean.DataLayer.csproj',
  },
  Api: {
    csproj: 'BeClean/BeClean.Api/BeClean.Api.csproj',
  },
  TestLib: {
    csproj: 'BeClean/BeClean.TestLib/BeClean.TestLib.csproj',
  },
};

const cfg = projects[project];

if (!cfg) {
  throw new Error(`Unknown project: "${project}". Valid values: ${Object.keys(projects).join(', ')}`);
}

module.exports = {
  branches: ['release'],
  tagFormat: `${project}\${version}`,
  plugins: [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
    ['@semantic-release/exec', {
      // Patch VersionPrefix in the csproj during the prepare lifecycle phase
      prepareCmd: `sed -i 's|<VersionPrefix>[^<]*</VersionPrefix>|<VersionPrefix>\${nextRelease.version}</VersionPrefix>|' ${cfg.csproj}`,
    }],
    ['@semantic-release/git', {
      // Commit the updated csproj back to main; [skip ci] prevents re-triggering the workflow
      assets: [cfg.csproj],
      message: `chore(release): ${project} \${nextRelease.version} [skip ci]`,
    }],
    ['@semantic-release/exec', {
      // Pack and push to NuGet.org in the publish lifecycle phase (after tag is created)
      publishCmd: [
        `dotnet pack ${cfg.csproj} -c Release -o ./nupkg/${project}`,
        `dotnet nuget push "./nupkg/${project}/*.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate`,
      ].join(' && '),
    }],
  ],
};
