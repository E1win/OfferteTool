(() => {
    const root = document.getElementById("questionnaire-editor");
    const bootstrapElement = document.getElementById("questionnaire-editor-bootstrap");
    const antiforgeryInput = document.querySelector("#questionnaire-antiforgery input[name='__RequestVerificationToken']");

    if (!root || !bootstrapElement || !window.Vue) {
        return;
    }

    const defaultErrorMessage = "Er ging iets mis bij het verwerken van uw verzoek.";
    const csrfErrorMessage = "Uw formulier kon niet veilig worden verwerkt. Vernieuw de pagina en probeer het opnieuw.";
    const unauthorizedErrorMessage = "U heeft geen toestemming om deze actie uit te voeren.";
    const sessionExpiredErrorMessage = "Uw sessie is verlopen. Meld u opnieuw aan en probeer het daarna nog eens.";

    const rawBootstrap = JSON.parse(bootstrapElement.textContent);
    const bootstrap = {
        apiBaseUrl: rawBootstrap.apiBaseUrl ?? "",
        antiforgeryHeaderName: rawBootstrap.antiforgeryHeaderName ?? "X-CSRF-TOKEN",
        canManageQuestions: rawBootstrap.canManageQuestions ?? false,
        questionTypes: {
            choice: rawBootstrap.questionTypes?.choice ?? "choice",
            text: rawBootstrap.questionTypes?.text ?? "text",
            numeric: rawBootstrap.questionTypes?.numeric ?? "numeric"
        }
    };
    const antiforgeryToken = antiforgeryInput?.value ?? "";

    if (!bootstrap.apiBaseUrl) {
        root.innerHTML = '<div class="alert alert-danger" role="alert">De vragenlijsteditor kon niet worden geladen. Vernieuw de pagina en probeer het opnieuw.</div>';
        return;
    }

    const createErrorState = (message, errors = []) => ({
        message,
        errors
    });

    const createEmptyOptions = () => [
        { id: null, text: "" },
        { id: null, text: "" }
    ];

    const createEmptyForm = (questionTypes) => ({
        text: "",
        score: "",
        type: questionTypes.choice,
        allowMultipleSelection: true,
        rows: 1,
        maxLength: "",
        minValue: "",
        maxValue: "",
        options: createEmptyOptions()
    });

    const createQuestionnaireState = (canManageQuestions) => ({
        isLoading: true,
        isSaving: false,
        errorMessage: "",
        errorDetails: [],
        canManageQuestions,
        questions: []
    });

    const createDialogState = (questionTypes) => ({
        isOpen: false,
        mode: "create",
        title: "Vraag toevoegen",
        submitLabel: "Toevoegen",
        errorMessage: "",
        errorDetails: [],
        questionId: null,
        form: createEmptyForm(questionTypes)
    });

    const resetErrors = (target) => {
        target.errorMessage = "";
        target.errorDetails = [];
    };

    const applyError = (target, error) => {
        target.errorMessage = error.message;
        target.errorDetails = error.errors ?? [];
    };

    const createDialogStateFromQuestion = (question, questionTypes) => ({
        isOpen: true,
        mode: "edit",
        title: "Vraag wijzigen",
        submitLabel: "Opslaan",
        errorMessage: "",
        errorDetails: [],
        questionId: question.id,
        form: {
            text: question.text,
            score: question.score ?? "",
            type: question.type,
            allowMultipleSelection: question.allowMultipleSelection,
            rows: question.rows ?? 1,
            maxLength: question.maxLength ?? "",
            minValue: question.minValue ?? "",
            maxValue: question.maxValue ?? "",
            options: question.options.length > 0
                ? question.options.map(option => ({ id: option.id, text: option.text }))
                : createEmptyOptions(questionTypes)
        }
    });

    const moveListItem = (items, index, direction) => {
        const targetIndex = index + direction;

        if (targetIndex < 0 || targetIndex >= items.length) {
            return false;
        }

        const [item] = items.splice(index, 1);
        items.splice(targetIndex, 0, item);
        return true;
    };

    const buildRequestOptions = (method, antiforgeryHeaderName, antiforgeryValue, body) => {
        const headers = {
            "Accept": "application/json"
        };

        if (method !== "GET") {
            headers[antiforgeryHeaderName] = antiforgeryValue;
        }

        if (body !== undefined) {
            headers["Content-Type"] = "application/json";
        }

        return {
            method,
            credentials: "same-origin",
            headers,
            body: body === undefined ? undefined : JSON.stringify(body)
        };
    };

    const extractErrorState = async (response) => {
        const contentType = response.headers.get("content-type") ?? "";

        try {
            const responseText = await response.text();

            if (contentType.includes("application/json") && responseText.length > 0) {
                const payload = JSON.parse(responseText);
                const errors = Array.isArray(payload?.errors)
                    ? payload.errors.filter(error => typeof error === "string" && error.length > 0)
                    : [];

                if (typeof payload?.message === "string" && payload.message.length > 0) {
                    return createErrorState(payload.message, errors);
                }

                if (payload?.errors) {
                    const firstError = Object.values(payload.errors).flat()[0];
                    if (typeof firstError === "string" && firstError.length > 0) {
                        return createErrorState(firstError, [firstError]);
                    }
                }
            }

            if (response.status === 401) {
                return createErrorState(sessionExpiredErrorMessage);
            }

            if (response.status === 403) {
                return createErrorState(unauthorizedErrorMessage);
            }

            if (response.status === 400 && responseText.toLowerCase().includes("antiforg")) {
                return createErrorState(csrfErrorMessage);
            }

            if (!contentType.includes("text/html") && responseText.trim().length > 0) {
                return createErrorState(responseText.trim());
            }
        } catch {
            return createErrorState(defaultErrorMessage);
        }

        return createErrorState(defaultErrorMessage);
    };

    const fetchApi = async (url, options) => {
        const response = await fetch(url, options);

        if (!response.ok) {
            throw await extractErrorState(response);
        }

        return response.json();
    };

    const app = Vue.createApp({
        data() {
            return {
                apiBaseUrl: bootstrap.apiBaseUrl,
                antiforgeryHeaderName: bootstrap.antiforgeryHeaderName,
                questionTypes: bootstrap.questionTypes,
                questionnaire: createQuestionnaireState(bootstrap.canManageQuestions),
                dialog: createDialogState(bootstrap.questionTypes)
            };
        },
        mounted() {
            this.loadQuestions();
        },
        methods: {
            setQuestions(questionState) {
                this.questionnaire.questions = questionState.questions ?? [];
            },
            openCreateDialog() {
                this.dialog = {
                    ...createDialogState(this.questionTypes),
                    isOpen: true
                };
            },
            openEditDialog(question) {
                this.dialog = createDialogStateFromQuestion(question, this.questionTypes);
            },
            closeDialog() {
                this.dialog = createDialogState(this.questionTypes);
            },
            addOption() {
                this.dialog.form.options.push({ id: null, text: "" });
            },
            removeOption(index) {
                this.dialog.form.options.splice(index, 1);
            },
            moveOption(index, direction) {
                moveListItem(this.dialog.form.options, index, direction);
            },
            toPayload() {
                const form = this.dialog.form;

                return {
                    text: form.text,
                    score: form.score === "" ? null : Number(form.score),
                    type: form.type,
                    allowMultipleSelection: form.allowMultipleSelection,
                    rows: form.type === this.questionTypes.text ? Number(form.rows || 1) : null,
                    maxLength: form.type === this.questionTypes.text && form.maxLength !== "" ? Number(form.maxLength) : null,
                    minValue: form.type === this.questionTypes.numeric && form.minValue !== "" ? Number(form.minValue) : null,
                    maxValue: form.type === this.questionTypes.numeric && form.maxValue !== "" ? Number(form.maxValue) : null,
                    options: form.type === this.questionTypes.choice
                        ? form.options.map(option => ({
                            id: option.id,
                            text: option.text
                        }))
                        : []
                };
            },
            createApiOptions(method, body) {
                return buildRequestOptions(
                    method,
                    this.antiforgeryHeaderName,
                    antiforgeryToken,
                    body);
            },
            async loadQuestions() {
                this.questionnaire.isLoading = true;
                resetErrors(this.questionnaire);

                try {
                    const payload = await fetchApi(
                        this.apiBaseUrl,
                        this.createApiOptions("GET"));

                    this.setQuestions(payload.data ?? {});
                } catch (error) {
                    applyError(this.questionnaire, error);
                } finally {
                    this.questionnaire.isLoading = false;
                }
            },
            async submitDialog() {
                this.questionnaire.isSaving = true;
                resetErrors(this.dialog);

                const url = this.dialog.mode === "create"
                    ? `${this.apiBaseUrl}/questions`
                    : `${this.apiBaseUrl}/questions/${this.dialog.questionId}`;
                const method = this.dialog.mode === "create" ? "POST" : "PUT";

                try {
                    await fetchApi(url, this.createApiOptions(method, this.toPayload()));
                    await this.loadQuestions();
                    this.closeDialog();
                } catch (error) {
                    applyError(this.dialog, error);
                } finally {
                    this.questionnaire.isSaving = false;
                }
            },
            async deleteQuestion(questionId) {
                if (!window.confirm("Weet u zeker dat u deze vraag wilt verwijderen?")) {
                    return;
                }

                this.questionnaire.isSaving = true;
                resetErrors(this.questionnaire);

                try {
                    await fetchApi(
                        `${this.apiBaseUrl}/questions/${questionId}`,
                        this.createApiOptions("DELETE"));
                    await this.loadQuestions();
                } catch (error) {
                    applyError(this.questionnaire, error);
                } finally {
                    this.questionnaire.isSaving = false;
                }
            },
            async moveQuestion(index, direction) {
                const orderedIds = this.questionnaire.questions.map(question => question.id);

                if (!moveListItem(orderedIds, index, direction)) {
                    return;
                }

                this.questionnaire.isSaving = true;
                resetErrors(this.questionnaire);

                try {
                    const payload = await fetchApi(
                        `${this.apiBaseUrl}/questions/reorder`,
                        this.createApiOptions("POST", { orderedQuestionIds: orderedIds }));

                    this.setQuestions(payload.data ?? {});
                } catch (error) {
                    applyError(this.questionnaire, error);
                } finally {
                    this.questionnaire.isSaving = false;
                }
            },
            scoreLabel(score) {
                return score ?? 0;
            }
        }
    });

    app.config.compilerOptions.delimiters = ["[[", "]]"];
    app.mount(root);
})();
