// ==UserScript==
// @name         RisuAIDeepL
// @namespace    https://risuai.xyz/
// @version      0.1
// @description  RisuAI DeepL 번역 스크립트
// @author       MM
// @match        https://risuai.xyz/
// @icon         https://www.google.com/s2/favicons?sz=64&domain=risuai.xyz
// @grant        none
// ==/UserScript==

(function() {
    const URL = "ws://localhost:5858/translate";

    document.body.addEventListener("click", (evt) => {
        if(document.getElementById("deeplInput") != null) return;

        const txtTranslate = document.getElementById("messageInputTranslate");
        if(txtTranslate == null) return;
        const txtInput = getInput();
        if(txtInput == null) return;

        const txtDeepL = txtTranslate.cloneNode();
        txtTranslate.style.display = "none";
        txtDeepL.id = "deeplInput";
        txtTranslate.after(txtDeepL);

        InitDeepL(txtDeepL, txtInput);
    });

    function getInput() {
        return document.querySelector("textarea.input-text:not(#messageInputTranslate)");
    }

    function InitDeepL(input, output) {
        const socket = new WebSocket(URL);
        let translates = [];
        let selected = 0;

        socket.onopen = (evt) => {
            input.addEventListener("input", (evt) => {
                if(evt.isComposing) return;
                socket.send(JSON.stringify({ text: input.value, to: "KO", from: "EN" }));
            });
        }

        socket.onmessage = (evt) => {
            translates = Array.from(JSON.parse(evt.data));
            output.value = translates[selected = 0];
        }
    }
})();