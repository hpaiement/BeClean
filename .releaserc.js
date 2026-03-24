'use strict';

const project = process.env.RELEASE_PROJECT;

if (!project) {
  throw new Error('RELEASE_PROJECT environment variable is required');
}

const validProjects = ['Util', 'Repository', 'Localization', 'DataLayer', 'Api', 'TestLib'];

if (!validProjects.includes(project)) {
  throw new Error(`Unknown project: "${project}". Valid values: ${validProjects.join(', ')}`);
}

module.exports = {
  branches: ['release'],
  tagFormat: `${project}\${version}`,
  plugins: [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
  ],
};
