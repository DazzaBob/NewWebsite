using System.Text;

namespace Website.App.HTML.Pages.Home
{
    public static class Index
    {
        public static string DeliveryModal(long userId)
        {
            StringBuilder sb = new();

            sb.Append("<div id=\"PDmodal\" class=\"custom-modal\">")
              .Append("<div id=\"PDmodalcontent\" class=\"custom-modal-content\" role=\"dialog\" aria-modal=\"true\" aria-labelledby=\"PDmodaltitle\" tabindex=\"-1\">");

            // Header
            sb.Append("<div id=\"PDmodalheader\" class=\"custom-modal-header\">")
              .Append("<h5 id=\"PDmodaltitle\">Package Delivery</h5>")
              .Append("<button id=\"PDmodalClose\" class=\"btn-close\" aria-label=\"Close\" onclick=\"closePDModal();\">×</button>")
              .Append("</div>");

            // Body
            sb.Append("<div id=\"PDmodalbody\" class=\"custom-modal-body\" style=\"display:flex; flex-direction:column; gap:1rem;\">");

            // Recipient input
            sb.Append("<div style=\"display:flex; align-items:center; gap:0.5rem; margin-top:0.5rem;\">")
                .Append("<label for=\"PDmodalRecipient\" style=\"white-space:nowrap; min-width:100px;\"><b>Recipient:</b></label>")
                .Append("<input type=\"text\" id=\"PDmodalRecipient\" class=\"input-text\" placeholder=\"Full Name\" ")
                .Append("style=\"flex:1; padding:0.5rem; border-radius:8px; border:1px solid var(--color-primary);\" required />")
                .Append("</div>");

            // Task input group

            sb.Append("<div id=\"PDtaskInputs\" style=\"display:flex; flex-direction:column; gap:0.5rem;\">");

            sb.Append("<div class=\"card address-card\">");
            sb.Append("<div style=\"display:flex; align-items:center; gap:0.5rem;\">")
                .Append("<input type=\"text\" id=\"PDmodalpickup\" class=\"address-edit-input input-text\" placeholder=\"Pickup Location / Address Type / Package Size\" required />")
                .Append("<a href=\"#\" onclick=\"placeholder()\" class=\"icon-btn\">")
                    .Append("<i class=\"fa-solid fa-home\"></i>")
                .Append("</a>")
            .Append("</div>");

            sb.Append("<span>&nbsp;</span>");

            sb.Append("<div style=\"display:flex; gap:1rem;\">"); // container for the two fields

            // Address Type
            //sb.Append("<div style=\"flex:1; display:flex; flex-direction:column; gap:0.25rem;\">")
            sb.Append("<div>")
              .Append("<label for=\"PDtasksPickupAddressTypeId\"><b>Address Type:</b></label>")
              .Append("<select id=\"PDtasksPickupAddressTypeId\" class=\"input-text\" name=\"PDtasksPickupAddressTypeId\" required>")
                  .Append("<option value=\"\">--Address type--</option>");
            foreach (var item in App.Helper.Table.AddressType.AddressTypeOptions())
            {
                sb.Append($"<option value=\"{item.Value}\">{item.Text}</option>");
            }
            sb.Append("</select>")
            .Append("</div>");

            // Package Size
            sb.Append("<div style=\"flex:1; display:flex; flex-direction:column; gap:0.25rem;\">")
                .Append("<label for=\"PDtasksPickupPackageSizeId\"><b>Package Size:</b></label>")
                .Append("<select id=\"PDtasksPickupPackageSizeId\" class=\"input-text\" name=\"PDtasksPickupPackageSizeId\" required >")
                    .Append("<option value=\"\">--Package size--</option>");
            //foreach (var item in GetPackageSizeOptions(locationConn)) // assuming you have a similar helper method
            //{
            //  sb.Append($"<option value=\"{item.Value}\">{item.Text}</option>");
            //}
            sb.Append("</select>")
            .Append("</div>");

            sb.Append("</div>"); // close flex container

            sb.Append("<span>&nbsp;</span>");

            // Ready time
            sb.Append("<div style=\"display:flex; flex-direction:column; gap:0.25rem;\">") // container for label + inputs
                .Append("<label for=\"PDmodalReadyTime\"><b>Ready:</b></label>") // label on top
                .Append("<div style=\"display:flex; align-items:center; gap:0.5rem;\">") // select + input on same line
                    .Append("<select id=\"PDmodalReadyTime\" style=\"padding:0.25rem; border-radius:4px; border:1px solid var(--color-primary);\">")
                        .Append("<option value=\"now\">Ready Now</option>")
                        .Append("<option value=\"specific\">Specific Time</option>")
                    .Append("</select>")
                    .Append("<input type=\"time\" id=\"PDmodalSpecificTime\" style=\"display:none; padding:0.25rem; border-radius:4px; border:1px solid var(--color-primary);\" />")
                .Append("</div>") // end flex container for select + input
            .Append("</div>"); // end main container

            sb.Append("</div>"); // close address card.


            sb.Append("<div style=\"display:flex; align-items:center; gap:0.5rem;\">")
                .Append("<input type=\"text\" id=\"PDmodaldropoff\" class=\"address-edit-input\" placeholder=\"Dropoff Location / Address Type\" style=\"width:100%; padding:0.5rem; border-radius:8px; border:1px solid var(--color-primary);\" required />")
                .Append("<a href=\"#\" onclick=\"placeholder()\" style=\"display:inline-flex; align-items:center; justify-content:center; padding:0.25rem; color:var(--color-primary); transition: transform 0.2s;\">")
                    .Append("<i class=\"fa-solid fa-home\"></i>") // grab the users default saved address.
                .Append("</a>")
            .Append("</div>");

            sb.Append("</div>"); // End task inputs

            // Task list display
            sb.Append("<div id=\"PDtaskList\" style=\"display:none; flex-direction:column; gap:0.5rem; margin-top:1rem;\"></div>");

            // Buttons
            sb.Append("<div style=\"display:flex; justify-content:space-between; gap:0.5rem; margin-top:1rem;\">")
              .Append("<button type=\"button\" id=\"PDmodalAddTask\" class=\"btn btn-primary\" onclick=\"addPDTask();\">Add Another</button>")
              .Append("<button type=\"button\" id=\"PDmodalAcceptTasks\" class=\"btn btn-success\" onclick=\"acceptPDTasks();\">Accept</button>")
              .Append("<button type=\"button\" id=\"PDmodalCancel\" class=\"btn btn-secondary\" onclick=\"closePDModal();\">Cancel</button>")
              .Append("</div>");

            sb.Append("</div>") // End modal body
              .Append("</div>") // End modal content
              .Append("</div>"); // End modal

            // Inline JS to toggle specific time input
            sb.Append("<script>")
              .Append("document.getElementById('PDmodalReadyTime').addEventListener('change', function(){")
              .Append("document.getElementById('PDmodalSpecificTime').style.display = this.value === 'specific' ? 'inline-block' : 'none';")
              .Append("});")
              .Append("</script>");

            return sb.ToString();
        }
    }
}
