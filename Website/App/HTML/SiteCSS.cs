using System.Text;
using System.Text.RegularExpressions;

namespace Website.App.HTML
{
    public static partial class SiteCSS
    {
        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
        public static void Create()
        {
            // Get the minified CSS string from SiteCSS
            // The path will always be wwwwroot/css/site.css
            string path = Path.Combine("wwwroot", "css", "site.css");
            if (File.Exists(path)) File.Delete(path);

            string cssContent = SiteCSS.Get();
            File.WriteAllText(path, cssContent);
        }
        private static string Get()
        {
            StringBuilder sb = new();
            sb.Append(Base());
            sb.Append(Layout());

            sb.Append(Elements());

            return Minify(sb.ToString());
        }
        private static string Minify(string CSS)
        {
            string minified = CSS.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("  ", "");

            return MyRegex().Replace(minified, " ").Trim();
        }
        #region Base
        private static string Base()
        {
            StringBuilder sb = new();
            sb.AppendLine(Theme());
            sb.AppendLine(DarkMode());
            sb.AppendLine(BaseElements());
            return sb.ToString();
        }
        // ───────────────────────────── Theme + Layout Summary ─────────────────────────────
        // This combined file contains:
        // 1. Theme Variables (:root)
        // 2. Dark Mode Overrides
        // 3. Font & Icon Imports
        // 4. Base Elements
        // 5. Layout: Header, Footer, Container
        // ──────────────────────────────────────────────────────────────────────────── */
        private static string Theme()
        {

            string css = @"
            :root {
            --font-family-base: system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            --color-primary: #ff5a5f;
            --color-primary-hover: #e14b50;
            --color-secondary: #007bff;
            --color-text: #333333;
            --btn-primary-bg: #ff5a5f;
            --btn-primary-hover-bg: #e14b50;
            --btn-primary-hover-text: #0;             
            --btn-primary-text: #ffffff; 
            --btn-secondary-bg: #007bff;
            --btn-secondary-hover-bg: #0056b3;
            --btn-secondary-text: #ffffff;
            --btn-transparent-bg: transparent;
            --btn-transparent-border: #ff5a5f;
            --btn-transparent-text: #ff5a5f;
            --btn-transparent-hover-bg: #ff5a5f;
            --btn-transparent-hover-text: #ffffff;
            --color-bg: #ffffff;
            --color-bg-alt: #f9f9f9;
            --color-bg-alt-alt: #f0f0f0;
            --color-header-bg: #ffffff;
            --color-footer-bg: #333333;
            --color-footer-text: #ffffff;
            --shadow-light: rgba(0,0,0,0.1);
            --shadow-strong: rgba(0,0,0,0.15);
            --hero-overlay-start: rgba(0,0,0,0.5);
            --hero-overlay-end: rgba(0,0,0,0.3);
            --input-height-mobile: 2.5rem;
            --input-height-tablet: 2.75rem;
            --input-height-desktop: 3rem;
            --top-bar-height: 3.5rem;
            --pill-bar-height: 2.5rem;
            --dash-header-height: calc(var(--top-bar-height)+var(--pill-bar-height));
            --dash-footer-height: 3.5rem;
            --spacing-small: .5rem;
            --spacing-medium: 1rem;
            --spacing-large: 2rem;
            --radius-medium: 0.5rem;
        }
            h1 { font-size: clamp(1.5rem, 4vw + 1rem, 3rem); }
            h2 { font-size: clamp(1.25rem, 3.5vw + 1rem, 2.5rem); }
            h3 { font-size: clamp(1.125rem, 3vw + 1rem, 2rem); }
            h4 { font-size: clamp(1rem, 2.5vw + 1rem, 1.75rem); }
            h5 { font-size: clamp(0.875rem, 2vw + 0.875rem, 1.5rem); }
            h6 { font-size: clamp(0.75rem, 1.5vw + 0.75rem, 1.25rem); }";
            return css;
        }
        private static string DarkMode()
        {
            string css = @"@media(prefers-color-scheme: dark) {
            :root {
            --color-text: #e1e1e1;
            --color-bg: #121212;
            --color-bg-alt: #1a1a1a;
            --color-bg-alt-alt: #222222;
            --color-header-bg: #1f1f1f;
            --color-footer-bg: #1a1a1a;
            }}";
            return css;
        }
        private static string BaseElements()
        {
            string css = @"
            body {
            margin: 0;
            padding: 0;
            font-family: var(--font-family-base);
            font-size: clamp(0.625rem, 2vw + 0.625rem, 2rem);
            line-height: 1.5;
            color: var(--color-text);
            background-color: var(--color-bg);
            display: flex;
            flex-direction: column;
            height: 100vh; /* full viewport height */
            overflow: hidden; /* prevent body scrolling */}
            *, *::before, *::after { box-sizing: border-box;}
            a {color: var(--color-primary); text-decoration: none;}
            a:hover {color: var(--color-primary-hover);}
            button {cursor: pointer;}";
           
            return css;
        }
        #endregion
        #region Layout
        private static string Layout()
        {
            StringBuilder sb = new();
            sb.AppendLine(Header());
            sb.AppendLine(MainContainer());
            sb.AppendLine(Footer());

            return sb.ToString();
        }
        //* ───────────────────────────── Layout Summary ─────────────────────────────
        // This file contains:
        // 1. Header
        // 2. Logo
        // 3. Navigation
        // 4. Main container
        // 5. Footer
        // 6. Responsive Breakpoints
        // ──────────────────────────────────────────────────────────────────────────── */
        private static string Header()
        {
            string css = @"
                .site-header 
                {display: flex; align-items: center; /* vertically centers content */
                justify-content: space-between;
                padding: 0.75rem .15rem; /* top/bottom and left/right padding */
                background: var(--color-header-bg);
                box-shadow: 0 2px 4px var(--shadow-light);
                position: fixed;
                width: 100%;
                top: 0;
                left: 0;
                margin: 0;
                box-sizing: border-box;
                z-index: 100;
                height: auto; /* let content dictate height */
                }
                .header-container 
                {width: 100%; margin: 0; padding: 0; display: flex; justify-content: space-between; align-items: center; box-sizing: border-box;}
                .logo {flex-shrink: 0;}
                .logo img
                {display: block; height: 40px; width: auto;}";
            return css;
        }
        private static string MainContainer()
        {
            string css = @".site-main {flex: 1 1 auto; width: 100%; margin: 0 auto; overflow-y: auto;}";
            return css;
        }
        private static string Footer()
        {
            string css = @"
                .site-footer 
                {background: var(--color-footer-bg); color: var(--color-footer-text);
                display: flex; align-items: center; justify-content: space-around;
                flex-shrink: 0; /* prevent shrinking */}
                .footer-container, footer 
                {width: 90%; max-width: 1200px; margin: 0 auto; padding: 1rem 1rem;
                text-align: center; font-size: clamp(0.625rem, 2vw + 0.625rem, 2rem);}
                .footer-container a, footer a 
                {color: var(--color-footer-text); margin: 0 0.5rem; font-weight: 300;
                transition: color 0.3s;}
                .footer-container a:hover, footer a:hover 
                {color: var(--color-primary);}
                /* Footer nav buttons */
                .footer-nav-btn 
                {flex: 1 1 120px; max-width: 200px; max-height: 60px; text-align: center;
                color: var(--color-text-secondary); text-decoration: none;
                padding: var(--spacing-small) 0 .25rem; display: flex; flex-direction: column;
                align-items: center; justify-content: flex-start;
                background: var(--color-bg-alt-alt); border: 1px solid var(--color-primary);
                border-radius: .25rem; transition: color 0.3s ease;}
                .footer-nav-btn i 
                {font-size: clamp(0.625rem, 2vw + 0.625rem, 2rem); margin-bottom: .25rem;
                transition: transform 0.3s ease, color 0.3s ease;
                font-family: 'Font Awesome 7 Free'; font-weight: 900;}
                .footer-nav-btn.active 
                {color: var(--color-primary);}
                .footer-nav-btn:hover 
                {color: var(--color-primary);}
                .footer-nav-btn:hover i
                {transform: rotate(15deg) scale(1.2); color: var(--color-primary);}";

            return css;
        }
        #endregion
        #region Elements
        private static string Elements()
        {
            StringBuilder sb = new();
            sb.AppendLine(Buttons());
            sb.AppendLine(GridCanvasCards());
            return sb.ToString();
        }
        private static string Buttons()
        {
            //<button class="btn btn-primary">Home</button>
            //<button class="btn btn-nav btn-outline">Settings</button>
            //<button class="btn btn-pill btn-transparent">Logout</button>
            string css = @".btn {display: inline-flex; align-items: center; justify-content: center; padding: 0.75rem 1.5rem; border-radius: 30px; border: none; font-weight: 600; font-size: clamp(0.875rem, 2vw + 0.875rem, 2rem); cursor: pointer; transition: all 0.3s ease; box-shadow: 0 4px 6px var(--shadow-light);}
            .btn-primary {background: var(--btn-primary-bg); color: var(--btn-primary-text);}
            .btn-primary:hover {background: var(--btn-primary-hover-bg); color: var(--btn-primary-hover-text); transform: translateY(-2px); box-shadow: 0 6px 8px var(--shadow-strong);}
            .btn-outline {background: var(--btn-transparent-bg); color: var(--btn-transparent-text); border: 2px solid var(--color-text);}
            .btn-outline:hover {background: var(--btn-transparent-hover-bg); color: var(--btn-transparent-hover-text); transform: translateY(-2px);}
            .btn-secondary {background: var(--btn-secondary-bg); color: var(--btn-secondary-text); border: 2px solid var(--btn-transparent-border);}
            .btn-secondary:hover {background: var(--btn-secondary-hover-bg); color: var(--btn-secondary-text); transform: translateY(-2px);}
            .btn-nav {padding: 0.35rem 0.9rem; border-radius: 15px; box-shadow: none; line-height: 1.2;}
            .btn-nav:hover {transform: translateY(-1px); box-shadow: none;}
            .btn-pill {padding: 0.35rem 0.9rem; font-size: clamp(0.70rem, 1.5vw + 0.70rem, 1.20rem); white-space: nowrap; border-radius: 9999px; box-shadow: 0 2px 4px var(--shadow-light); line-height: 1.2;}
            .btn-pill:hover {transform: translateY(-1px); box-shadow: 0 4px 6px var(--shadow-strong);}
            .btn-pill-disabled {opacity: .5; pointer-events: none;}";

            return css;
        }
        private static string GridCanvasCards()
        {
            string css = @".grid-canvas {width: 100%; background-color: var(--color-bg-alt); padding: var(--spacing-medium); box-sizing: border-box;}
            .grid {display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: var(--spacing-medium); align-items: stretch; width: 100%}
            .card {background-color: var(--color-bg-alt-alt); border: 2px solid var(--color-primary); border-radius: var(--radius-medium); box-shadow: 0 2px 4px var(--shadow-light); display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 1rem; gap: 0.5rem; box-sizing: border-box;}
            .card h3 {margin: 0 0 0.5rem 0; font-weight: 600; font-size: clamp(1rem, 2vw + 0.875rem, 1.5rem); text-align: center;}
            .card p, .card b, .card i, .card ul, .card blockquote {margin: 0; text-align: center;}";
            
            return css;
        }
        #endregion
    }
}