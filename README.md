# UTransfer

## Description
UTransfer is a fast file transfer application designed to send and receive large files between computers that are geographically distant. This is achieved by using **Hamachi** or by manually **opening port 5001** for direct connections.

## Main Features
- Send and receive files between computers over the internet using Hamachi or port forwarding.
- Progress tracking with a loading bar.
- Real-time speed display (in MB/s).
- Cancel transfers at any time from either the sender or receiver.
- Optimized for fast transfers with a 512 KB buffer.

## Requirements
- Operating System: **Windows 10 or later**
- Framework: **.NET Framework v8.0.x**

## Installation
1. Download the project executable.
2. Ensure the .NET Framework is installed.
3. Run the application using the provided executable.

## Usage Instructions
1. **Using Hamachi or Opening Port 5001**:
   - The app is designed specifically to work over long-distance connections, not just local networks.
   - Use **Hamachi** to create a virtual LAN, or **open port 5001** on your network to allow direct file transfers.

2. **Sending a File**:
   - Click on the "Send" button in the main interface.
   - Select a file from your system via the file explorer.
   - Enter the recipient's IP address (through Hamachi or an open port).
   - Click "Send" and monitor the progress using the loading bar.

3. **Receiving a File**:
   - Click on the "Receive" button to start the receiving server.
   - The server will wait for a connection from a sender.
   - Once the file arrives, a confirmation dialog will appear to accept or reject the transfer.
   - If accepted, the transfer begins, and the progress is visible via the loading bar.

4. **Canceling a Transfer**:
   - A transfer can be canceled at any time by either the sender or the recipient.
   - If canceled by the sender, any partially received file will be automatically deleted.

## Known Issues
- The progress bar may not reset correctly after canceling a transfer.
- Improvements for handling forced application closures are under development.

## Upcoming Features
- Complete interface remake in English.
- Enhanced handling of forced application closures to prevent unsaved states.

## Authors
- **Palawi**: C# migration and development of all post-migration features.
- **Statix**: Original idea, initial Python version development, general support.
- **Aliz**: emotional support

## Contact
For any questions, suggestions, or bug reports, please contact: **palawi.pro@gmail.com**
