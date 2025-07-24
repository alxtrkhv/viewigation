set unstable
set script-interpreter := ['uv', 'run', '--script']

[private]
default:
  @just --list

[script]
set-version new_version:
    import re
    import subprocess
    import sys
    from pathlib import Path
    import shutil

    new_version = "{{ new_version }}"
    package_json_path = Path("Viewigation.Unity/package.json")
    readme_path = Path("README.md")
    unity_readme_path = Path("Viewigation.Unity/README.md")
    tag_name = f"release/v{new_version}"
    commit_message = f"release: v{new_version}"

    print("Checking git status...")
    try:
        status_result = subprocess.run(["git", "status", "--porcelain"], check=True, capture_output=True, text=True)
        if status_result.stdout:
            print("Error: Git working directory is not clean. Please commit or stash changes.")
            print(status_result.stdout)
            sys.exit(1)
        print("Git status is clean.")

        print(f"Setting version to {new_version} in {package_json_path}...")
        package_content = package_json_path.read_text()
        updated_package_content, package_count = re.subn(
            r'("version":\s*")(.*?)(")',
            rf'\g<1>{new_version}\g<3>',
            package_content,
            count=1
        )

        if package_count == 0:
            print(f"Error: Could not find version field in {package_json_path}")
            sys.exit(1)

        package_json_path.write_text(updated_package_content)
        print(f"Successfully updated {package_json_path}")

        print(f"Updating UPM install git URL to point to v{new_version}...")
        readme_content = readme_path.read_text()
        updated_readme_content, readme_count = re.subn(
            r'(https://github\.com/alxtrkhv/viewigation\.git\?path=/Viewigation\.Unity#release/v)(.*?)(\n)',
            rf'\g<1>{new_version}\g<3>',
            readme_content,
            count=1
        )

        if readme_count == 0:
            print(f"Error: Could not find UPM git URL in {readme_path}")
            sys.exit(1)

        readme_path.write_text(updated_readme_content)
        print(f"Successfully updated UPM git URL in {readme_path}")

        print(f"Copying {readme_path} to {unity_readme_path}...")
        shutil.copy2(readme_path, unity_readme_path)
        print(f"Successfully copied README.md to {unity_readme_path}")

        print(f"Staging {package_json_path}, {readme_path}, and {unity_readme_path}...")
        subprocess.run(["git", "add", str(package_json_path), str(readme_path), str(unity_readme_path)], check=True)
        print(f"Successfully staged all files")

        print(f"Creating git commit with message '{commit_message}'...")
        subprocess.run(["git", "commit", "--allow-empty", "-m", commit_message], check=True, capture_output=True, text=True)
        print("Successfully created git commit.")

        print(f"Creating git tag {tag_name}...")
        subprocess.run(["git", "tag", tag_name], check=True, capture_output=True, text=True)
        print(f"Successfully created git tag {tag_name}")

    except FileNotFoundError as e:
        if "package.json" in str(e):
            print(f"Error: {package_json_path} not found.")
        elif "README.md" in str(e):
            print(f"Error: {readme_path} not found.")
        else:
            print(f"Error: File not found - {e}")
        sys.exit(1)
    except subprocess.CalledProcessError as e:
        command = " ".join(e.cmd)
        print(f"Error executing git command '{command}':")
        print(e.stderr)
        sys.exit(1)
    except subprocess.CalledProcessError as e:
        print(f"Error creating git tag {tag_name}:")
        print(e.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"An unexpected error occurred: {e}")
        sys.exit(1)
