using RTools_NTS.Util;
using System.Text;

namespace Website.App.Scripts
{
    public static class Mapbox
    {
        private static string autocompletescript = string.Empty;

        public static string AutoCompleteScript(string mapboxToken, string inputId = "NewAddressSearch", bool forceRebuild = false)
        {
            if (autocompletescript == string.Empty || forceRebuild)
            {
                StringBuilder sb = new();
                sb.Append("(() => {");
                sb.Append($"const t='{mapboxToken}';");
                sb.Append($"const a=document.getElementById('{inputId}');");
                sb.Append("const b=document.getElementById('NewAutocompleteList');");
                sb.Append("const c=document.getElementById('NewMapboxAddressJSON');");
                sb.Append("let d=null;");
                sb.Append("function e(fn,d=300){let t;return(...a)=>{clearTimeout(t);t=setTimeout(()=>fn(...a),d);};}");
                sb.Append("const f=e(async()=>{const q=a.value.trim();if(!q)return b.hidden=true;d?.abort();d=new AbortController();");
                sb.Append("const u=`https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(q)}.json?autocomplete=true&country=nz&limit=5&access_token=${t}`;");
                sb.Append("try{const r=await fetch(u,{signal:d.signal});const{features}=await r.json();g(features||[]);}catch(e){if(e.name!=='AbortError')console.error(e);}},200);");
                sb.Append("function g(f){b.innerHTML='';if(!f.length)return b.hidden=true;f.forEach(x=>{const li=document.createElement('li');li.textContent=x.place_name;li.addEventListener('mousedown',()=>h(x));b.appendChild(li);});b.hidden=false;}");
                sb.Append("function h(x){a.value=x.place_name;c.value=JSON.stringify(x);b.hidden=true;}");
                sb.Append("a.addEventListener('input',f);document.addEventListener('click',e=>{if(!a.contains(e.target)&&!b.contains(e.target))b.hidden=true;});");
                sb.Append("})();");
                autocompletescript = sb.ToString();
            }
            return autocompletescript;
        }
    }
}
