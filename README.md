# Trickplay Folder Cleaner

A Jellyfin plugin that automatically cleans up orphaned `.trickplay` folders.

## Summary

When media files are deleted, moved, renamed or replaced, their associated `.trickplay` folders (used for scrubbing/preview thumbnails) are not deleted by Jellyfin and consum disk space. This plugin adds a scheduled task that scans your libraries and deletes these orphaned folders.

## Features

- **Automated Cleanup**: Adds a maintenance task to Jellyfin's Scheduled Tasks.
- **Safe Deletion**: Only deletes `.trickplay` folders that have no corresponding media file in the same directory.
- **Configurable Schedule**: Defaults to running every Sunday at 2:00 AM, but can be customized in Jellyfin's task settings.

## How It Works

The plugin scans all configured virtual folders in your Jellyfin library. For every directory named `[filename].trickplay`, it checks for the existence of a media file named `[filename]` with a supported video extension (e.g., `.mkv`, `.mp4`, `.avi`, etc.) in the parent directory. If no matching media file is found, the `.trickplay` folder is deleted.

## Installation

### Manual Installation

Add this repository in Jellyfin: Plugins -> Manage Repositories -> Add Repository:

> https://raw.githubusercontent.com/Noir1992/jellyfin-trickplay-folder-cleaner/main/manifest.json

After that, go back, install the plugin and restart Jellyfin.

## Usage

1. Navigate to the **Dashboard** in Jellyfin.
2. Go to **Scheduled Tasks**.
3. Look for **Trickplay Folder Cleaner** under the **Maintenance** category.
4. You can trigger the task manually by clicking the "Play" button or configure the trigger schedule as needed.

## Check if it worked

The plugin logs every entry that it deletes. This means that you can check the logs to see what folders it deleted. The following logs can be found:
- Starting trickplay folder cleanup.
- Deleting orphaned trickplay folder: "<full_path_to_folder>.trickplay"
- Trickplay folder cleanup finished. Deleted \<NUMBER> folders.

## Supported Media Extensions

The plugin checks for the following extensions to verify if a media file exists:
`.mkv`, `.mp4`, `.avi`, `.mov`, `.wmv`, `.m4v`, `.ts`, `.mts`, `.m2ts`, `.mpg`, `.mpeg`, `.webm`, `.flv`, `.3gp`, `.ogv`
