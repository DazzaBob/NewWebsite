using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Website.App.Scripts.Pages.User.Dashboard
{
    public static class Index
    {
        private static string closecurrentaddressmodal = string.Empty;
        private static string closenewaddresscard = string.Empty;
        private static string resetnewaddressinputs = string.Empty;
        public static string CloseCurrentAddressModal(bool forceReload = false)
        {
            if (closecurrentaddressmodal == string.Empty || forceReload)
            {
                StringBuilder sb = new();
                sb.Append("function closeAddressModal() ");
                sb.Append('{');
                sb.Append("ResetNewAddressInputs(); ");

                sb.Append("const section = document.getElementById('addaddresssection'); ");
                sb.Append("if (section) section.style.display = 'none'; ");

                //sb.Append("const modal = document.getElementById('addressModal'); ");
                //sb.Append("if (modal) ");
                //sb.Append('{');
                //sb.Append("modal.scrollTop = 0; ");
                //sb.Append("modal.style.display = 'none'; "); // or remove 'show' class if you're using Bootstrap
                //sb.Append('}');
                sb.AppendLine("} ");
                closecurrentaddressmodal = sb.ToString();
            }
            return closecurrentaddressmodal;
        }
        public static string ResetNewAddressInputs(bool forceReload = false)
        {
            if (resetnewaddressinputs == string.Empty || forceReload)
            {
                StringBuilder sb = new();
                sb.Append("function ResetNewAddressInputs() {");
                sb.Append("const label = document.getElementById('newLabel'); ");
                sb.Append("const search = document.getElementById('NewAddressSearch'); ");
                sb.Append("const payload = document.getElementById('NewMapboxAddressJSON'); ");
                sb.Append("const type = document.getElementById('NewAddressTypeID'); ");
                sb.Append("const list = document.getElementById('NewAutocompleteList'); ");
                sb.Append("if (label) label.value = ''; ");
                sb.Append("if (search) search.value = ''; ");
                sb.Append("if (payload) payload.value = ''; ");
                sb.Append("if (type) type.selectedIndex = 0; ");
                sb.Append("if (list) list.hidden = true; ");
                sb.AppendLine("} ");
                resetnewaddressinputs = sb.ToString();
            }
            return resetnewaddressinputs;
        }
        public static string CloseNewAddressCard(bool forceReload = false)
        {
            if (closenewaddresscard == string.Empty || forceReload)
            {
                StringBuilder sb = new();
                sb.Append("function CloseNewAddressCard() {");
                sb.Append("ResetNewAddressInputs(); ");
                sb.Append("const btnCancel = document.getElementById('addressCancel'); ");
                sb.Append("const btnSave = document.getElementById('addressSave'); ");
                sb.Append("const section = document.getElementById('addaddresssection'); ");
                sb.Append("if (btnCancel) btnCancel.style.display = \"block\"; ");
                sb.Append("if (btnSave) btnSave.style.display = \"block\"; ");
                sb.Append("if (section) section.style.display = 'none'; ");
                sb.AppendLine("} ");
                closenewaddresscard = sb.ToString();
            }
            return closenewaddresscard;
        }
    }
}
