
# Simple Web Server

This is a simple web server implemented in C# that serves `index.html` files and static assets from specific directories. The server automatically scans directories for `index.html` files and creates listeners on unique ports for each one. Additionally, the server supports directory browsing when no `index.html` files are found and can open the served URLs in the default web browser.

## Features

- **Automatic Scanning**: Scans the current directory and its subdirectories for `index.html` files.
- **Unique Ports**: Creates an HTTP listener on a unique port for each `index.html` file found.
- **Directory Browsing**: If no `index.html` files are found, the server will provide a directory listing for browsing.
- **Log and Debug Mode**: A debug mode can be enabled to show detailed request handling and log entries.
- **Auto-Launch Browser**: Automatically opens the served URLs in the default web browser.
  
## Requirements

- **.NET Framework** or **.NET Core/5+** (ensure `Process.Start()` works for opening the browser in your environment).

## Installation

Clone the repository to your local machine:

```bash
git clone https://github.com/your-repo/simple-web-server.git
```

Navigate to the project directory:

```bash
cd simple-web-server
```

Build the project using your preferred C# development environment or via the command line:

```bash
dotnet build
```

## Usage

Run the executable from the command line:

```bash
simpleWebServer.exe
```

By default, the server will scan for `index.html` files and start serving them on unique ports. If no `index.html` files are found, the server will enable directory browsing.

### Debug Mode

To enable detailed logging and debug mode, run the application with the `--debug` flag:

```bash
simpleWebServer.exe --debug
```

### Opening in a Web Browser

The application will automatically launch the URLs in your default web browser for each listener created. The URLs will also be printed in the console as clickable links.

### Example Output

```bash
Listening on http://localhost:8080/ (click to open) for files in D:\Projects\Documentation\EngineeringHelp
Listening on http://localhost:8081/ (click to open) for files in D:\Projects\Documentation\OperatingHelp
Press any key to stop the servers...
```

## Logging

If `--debug` is enabled, detailed logs will be written to both the console and a file (`server_log.txt`). The logs include information such as:

- Server startup messages
- HTTP request handling
- 404 errors (when a requested file is not found)
- Detailed responses for each served file

## Project Structure

- `Program.cs`: The main application file containing the server logic.
- `README.md`: This readme file.
- `server_log.txt`: The log file created when `--debug` mode is enabled.

## How It Works

1. **Scanning for `index.html` Files**:
   - The server scans the current directory and its subdirectories for `index.html` files.
   - For each file found, a listener is created on a unique port, and the corresponding root directory is used to serve the files.
   
2. **Serving Static Files**:
   - If a client requests a file (e.g., `assets/js/25-collapsible.js`), the server looks for it in the root directory of the listener.
   - The file path is normalized for Windows systems using backslashes.

3. **Directory Browsing**:
   - If no `index.html` files are found, the server defaults to directory browsing mode, allowing users to navigate the file structure.

4. **Browser Launching**:
   - For each URL, the server automatically opens the default web browser with the correct URL.

## Customization

- **Port Configuration**: The server starts on port `8080` and increments the port number for each additional listener. You can modify the `startingPort` variable in the code to change the starting port.
- **Log Path**: The log file (`server_log.txt`) can be customized by changing the `logFilePath` variable in the code.

## License

This project is licensed under the **GNU General Public License, Version 2 (GPLv2)**. See the `LICENSE` file for details.
