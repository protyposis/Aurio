// `npx commit-and-tag-version`

const buildprops = {
  filename: "src/Directory.Build.props",
  type: "csproj",
};

module.exports = {
  bumpFiles: [buildprops],
  packageFiles: [buildprops],
};
