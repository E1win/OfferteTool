// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

if (window.jQuery?.validator) {
    jQuery.extend(jQuery.validator.messages, {
        required: "Dit veld is verplicht.",
        remote: "Controleer dit veld.",
        email: "Vul een geldig e-mailadres in.",
        url: "Vul een geldige URL in.",
        date: "Vul een geldige datum in.",
        dateISO: "Vul een geldige datum in (jjjj-mm-dd).",
        number: "Vul een geldig getal in.",
        digits: "Gebruik alleen cijfers.",
        equalTo: "De ingevulde waarden komen niet overeen.",
        maxlength: jQuery.validator.format("Gebruik maximaal {0} tekens."),
        minlength: jQuery.validator.format("Gebruik minimaal {0} tekens."),
        rangelength: jQuery.validator.format("Gebruik tussen de {0} en {1} tekens."),
        range: jQuery.validator.format("Vul een waarde in tussen {0} en {1}."),
        max: jQuery.validator.format("Vul een waarde in kleiner dan of gelijk aan {0}."),
        min: jQuery.validator.format("Vul een waarde in groter dan of gelijk aan {0}.")
    });
}
