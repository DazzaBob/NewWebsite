window.Platform = window.Platform || {};
Platform.Dashboards = Platform.Dashboards || {};
Platform.Dashboards.User = Platform.Dashboards.User || {};

Platform.Dashboards.User.Address = (function () {
    console.log("Platform.Dashboards.User.Address loaded");

    const selectors = {
        addLink: "#add-address-link",
        newCard: "#new-address-card",
        cancelBtn: "#cancelNewAddress"
    };

    function init() {
        const addLink = document.querySelector("#add-address-link");
        //const addLink = document.querySelector(selectors.addLink);
        const newCard = document.querySelector(selectors.newCard);
        const cancelBtn = document.querySelector(selectors.cancelBtn);
        console.log("Platform.Dashboards.User.Address.init() called");

        console.log("addLink:", addLink);
        addLink.addEventListener("click", function (e) {
            console.log("Click event triggered");
            e.preventDefault();
            addLink.style.display = "none";
            newCard.style.display = "block";
            console.log("Add Address clicked");
        });

        if (addLink && newCard) {
            addLink.addEventListener("click", function (e) {
                e.preventDefault();
                addLink.style.display = "none";
                newCard.style.display = "block";
                console.log("Add Address clicked");
            });
        }

        if (cancelBtn && newCard && addLink) {
            cancelBtn.addEventListener("click", function () {
                newCard.style.display = "none";
                addLink.style.display = "inline-block";
                console.log("Cancel clicked");
            });
        }
    }

    return { init };
})();