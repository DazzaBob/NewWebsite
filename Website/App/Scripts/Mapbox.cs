using System.Text;

namespace Website.App.Scripts
{
    public static class Mapbox
    {
        public static string AutoCompleteScript(string textInput, string autoCompleteList, string hiddenInputJson)
        {
            StringBuilder sb = new();
            sb.Append("(() => {")
            .Append("const t=document.querySelector('meta[name=\"t\"]').content; ")
            .Append($"const a=document.getElementById('{textInput}'); ")
            .Append($"const b=document.getElementById('{autoCompleteList}'); ")
            .Append($"const c=document.getElementById('{hiddenInputJson}'); ")
            .Append("let d=null; ")
            .Append("function e(fn,d=300){let t;return(...a)=>{clearTimeout(t);t=setTimeout(()=>fn(...a),d);};} ")
            .Append("const f=e(async()=>{const q=a.value.trim();if(!q)return b.hidden=true;d?.abort();d=new AbortController(); ")
            .Append("const u=`https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(q)}.json?autocomplete=true&country=nz&limit=5&access_token=${t}`; ")
            .Append("try{const r=await fetch(u,{signal:d.signal});const{features}=await r.json();g(features||[]);}catch(e){if(e.name!=='AbortError')console.error(e);}},200); ")
            .Append("function g(f){b.innerHTML='';if(!f.length)return b.hidden=true;f.forEach(x=>{const li=document.createElement('li');li.textContent=x.place_name;li.addEventListener('mousedown',()=>h(x));b.appendChild(li);});b.hidden=false;}")
            .Append("function h(x){a.value=x.place_name;c.value=JSON.stringify(x);b.hidden=true;}")
            .Append("a.addEventListener('input',f);document.addEventListener('click',e=>{if(!a.contains(e.target)&&!b.contains(e.target))b.hidden=true;});")
            .AppendLine("})();");

            return sb.ToString();
        }       
    }
}
