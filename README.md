# ModsWatcher

**ModsWatcher** is a mod library manager and version tracker designed for any app or game but was inspired by simulation games like **American Truck Simulator (ATS)** and **Euro Truck Simulator 2 (ETS2)**. It uses a built-in browser engine to "watch" mod pages and notify you the moment a new version is available.

---

## 🚀 Getting Started

### Installation
1. Download the latest `ModsWatcher.zip` from the [Releases](https://github.com/AdhamAbdelhameedElsharkawy/mods-watcher/releases) section.
2. Extract the ZIP to a folder (e.g., `C:\Games\ModsWatcher`).
3. Launch `ModsWatcher.Desktop.exe`.

> **Initial Setup:** On the first launch, the app will download the Chromium engine (approx. 130 seconds). A setup window will track the progress.

---

## 🛠 How to Use

### 1. Register an Application
Before watching mods, you need to add the game or app they belong to.
* Go to the **App Selection** screen.
* Click **Add New App**.
* Enter the name (e.g., "ATS") and other information.

### 2. Add a Mod to Watch
Once an app is selected, you can start building your library:
* Click **Add Mod**.
* Provide the **Source URL** (e.g., a Nexus Mods or Forum page).
* Enter the **Mod Name** and your **Current Version**.

### 3. Setup the "Watcher" (XPath)
The "Watcher" is the brain of the app. It goes to the mod page and looks for the "Latest Version" string using an **XPath**.
* **Find the XPath:** Right-click the version number on the mod's website in your browser, select **Inspect**, right-click the highlighted code, and choose **Copy > Copy XPath**.
* **Paste in App:** Paste that value into the "Version XPath" field in ModsWatcher.
* **Crawl:** Click the **Crawl** button. The app will launch a headless browser, find the text at that location, and save it to your database.

### 4. Tracking Updates
* The **Library View** displays all installed mods.
* App cards show a summary, including the **number of mods used** and pending updates.
* When a crawl detects a version mismatch, the mod will be flagged so you know exactly what needs updating.

---

## 🏗 Technical Details
* **Core:** .NET 10.0 / WPF
* **Browser Engine:** Playwright (Chromium)
* **Database:** SQLite (Local)
* **Architecture:** Self-contained sidecar (no external dependencies required).

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
