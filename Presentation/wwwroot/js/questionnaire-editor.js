(() => {
    const root = document.getElementById("questionnaire-editor");
    const bootstrapElement = document.getElementById("questionnaire-editor-bootstrap");
    const antiforgeryInput = document.querySelector("#questionnaire-antiforgery input[name='__RequestVerificationToken']");

    if (!root || !bootstrapElement || !window.Vue) {
        return;
    }

    const rawBootstrap = JSON.parse(bootstrapElement.textContent);
    const bootstrap = {
        apiBaseUrl: rawBootstrap.apiBaseUrl ?? rawBootstrap.ApiBaseUrl ?? "",
        antiforgeryHeaderName: rawBootstrap.antiforgeryHeaderName ?? rawBootstrap.AntiforgeryHeaderName ?? "X-CSRF-TOKEN",
        canManageQuestions: rawBootstrap.canManageQuestions ?? rawBootstrap.CanManageQuestions ?? false,
        tenderId: rawBootstrap.tenderId ?? rawBootstrap.TenderId ?? null,
        questionTypes: {
            choice: rawBootstrap.questionTypes?.choice ?? rawBootstrap.QuestionTypes?.Choice ?? "choice",
            text: rawBootstrap.questionTypes?.text ?? rawBootstrap.QuestionTypes?.Text ?? "text",
            numeric: rawBootstrap.questionTypes?.numeric ?? rawBootstrap.QuestionTypes?.Numeric ?? "numeric"
        }
    };
    const antiforgeryToken = antiforgeryInput?.value ?? "";

    if (!bootstrap.apiBaseUrl) {
        root.innerHTML = '<div class="alert alert-danger" role="alert">De vragenlijsteditor kon niet worden geladen. Vernieuw de pagina en probeer het opnieuw.</div>';
        return;
    }

    const createEmptyForm = () => ({
        text: "",
        score: "",
        type: bootstrap.questionTypes.choice,
        allowMultipleSelection: true,
        rows: 1,
        maxLength: "",
        minValue: "",
        maxValue: "",
        options: [
            { id: null, text: "" },
            { id: null, text: "" }
        ]
    });

    const createErrorState = (message, errors = []) => ({
        message,
        errors
    });

    const extractErrorState = async (response) => {
        const fallbackMessage = "Er ging iets mis bij het verwerken van uw verzoek.";
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
                return createErrorState("Uw sessie is verlopen. Meld u opnieuw aan en probeer het daarna nog eens.");
            }

            if (response.status === 403) {
                return createErrorState("U heeft geen toestemming om deze actie uit te voeren.");
            }

            if (response.status === 400 && responseText.toLowerCase().includes("antiforg")) {
                return createErrorState("Uw formulier kon niet veilig worden verwerkt. Vernieuw de pagina en probeer het opnieuw.");
            }

            if (!contentType.includes("text/html") && responseText.trim().length > 0) {
                return createErrorState(responseText.trim());
            }
        } catch {
            return createErrorState(fallbackMessage);
        }

        return createErrorState(fallbackMessage);
    };

    const app = Vue.createApp({
        data() {
            return {
                apiBaseUrl: bootstrap.apiBaseUrl,
                antiforgeryHeaderName: bootstrap.antiforgeryHeaderName,
                questionTypes: bootstrap.questionTypes,
                questionnaire: {
                    isLoading: true,
                    isSaving: false,
                    errorMessage: "",
                    errorDetails: [],
                    canManageQuestions: bootstrap.canManageQuestions,
                    questions: []
                },
                dialog: {
                    isOpen: false,
                    mode: "create",
                    title: "Vraag toevoegen",
                    submitLabel: "Toevoegen",
                    errorMessage: "",
                    errorDetails: [],
                    questionId: null,
                    form: createEmptyForm()
                }
            };
        },
        mounted() {
            this.loadQuestions();
        },
        methods: {
            async loadQuestions() {
                this.questionnaire.isLoading = true;
                this.questionnaire.errorMessage = "";
                this.questionnaire.errorDetails = [];

                try {
                    const response = await fetch(this.apiBaseUrl, {
                        credentials: "same-origin",
                        headers: {
                            "Accept": "application/json"
                        }
                    });

                    if (!response.ok) {
                        throw await extractErrorState(response);
                    }

                    const payload = await response.json();
                    const state = payload.data ?? {};
                    this.questionnaire.questions = state.questions ?? [];
                    this.questionnaire.canManageQuestions = (state.canManageQuestions ?? false) && bootstrap.canManageQuestions;
                } catch (error) {
                    this.questionnaire.errorMessage = error.message;
                    this.questionnaire.errorDetails = error.errors ?? [];
                } finally {
                    this.questionnaire.isLoading = false;
                }
            },
            openCreateDialog() {
                this.dialog.isOpen = true;
                this.dialog.mode = "create";
                this.dialog.title = "Vraag toevoegen";
                this.dialog.submitLabel = "Toevoegen";
                this.dialog.errorMessage = "";
                this.dialog.errorDetails = [];
                this.dialog.questionId = null;
                this.dialog.form = createEmptyForm();
            },
            openEditDialog(question) {
                this.dialog.isOpen = true;
                this.dialog.mode = "edit";
                this.dialog.title = "Vraag wijzigen";
                this.dialog.submitLabel = "Opslaan";
                this.dialog.errorMessage = "";
                this.dialog.errorDetails = [];
                this.dialog.questionId = question.id;
                this.dialog.form = {
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
                        : [{ id: null, text: "" }, { id: null, text: "" }]
                };
            },
            closeDialog() {
                this.dialog.isOpen = false;
                this.dialog.mode = "create";
                this.dialog.title = "Vraag toevoegen";
                this.dialog.submitLabel = "Toevoegen";
                this.dialog.errorMessage = "";
                this.dialog.errorDetails = [];
                this.dialog.questionId = null;
                this.dialog.form = createEmptyForm();
            },
            addOption() {
                this.dialog.form.options.push({ id: null, text: "" });
            },
            removeOption(index) {
                this.dialog.form.options.splice(index, 1);
            },
            moveOption(index, direction) {
                const newIndex = index + direction;
                if (newIndex < 0 || newIndex >= this.dialog.form.options.length) {
                    return;
                }

                const [item] = this.dialog.form.options.splice(index, 1);
                this.dialog.form.options.splice(newIndex, 0, item);
            },
            async submitDialog() {
                this.questionnaire.isSaving = true;
                this.dialog.errorMessage = "";
                this.dialog.errorDetails = [];

                try {
                    const url = this.dialog.mode === "create"
                        ? `${this.apiBaseUrl}/questions`
                        : `${this.apiBaseUrl}/questions/${this.dialog.questionId}`;

                    const response = await fetch(url, {
                        method: this.dialog.mode === "create" ? "POST" : "PUT",
                        credentials: "same-origin",
                        headers: {
                            "Accept": "application/json",
                            "Content-Type": "application/json",
                            [this.antiforgeryHeaderName]: antiforgeryToken
                        },
                        body: JSON.stringify(this.toPayload())
                    });

                    if (!response.ok) {
                        throw await extractErrorState(response);
                    }

                    await response.json();
                    await this.loadQuestions();
                    this.closeDialog();
                } catch (error) {
                    this.dialog.errorMessage = error.message;
                    this.dialog.errorDetails = error.errors ?? [];
                } finally {
                    this.questionnaire.isSaving = false;
                }
            },
            async deleteQuestion(questionId) {
                if (!window.confirm("Weet u zeker dat u deze vraag wilt verwijderen?")) {
                    return;
                }

                this.questionnaire.isSaving = true;
                this.questionnaire.errorMessage = "";
                this.questionnaire.errorDetails = [];

                try {
                    const response = await fetch(`${this.apiBaseUrl}/questions/${questionId}`, {
                        method: "DELETE",
                        credentials: "same-origin",
                        headers: {
                            "Accept": "application/json",
                            [this.antiforgeryHeaderName]: antiforgeryToken
                        }
                    });

                    if (!response.ok) {
                        throw await extractErrorState(response);
                    }

                    await response.json();
                    await this.loadQuestions();
                } catch (error) {
                    this.questionnaire.errorMessage = error.message;
                    this.questionnaire.errorDetails = error.errors ?? [];
                } finally {
                    this.questionnaire.isSaving = false;
                }
            },
            async moveQuestion(index, direction) {
                const newIndex = index + direction;
                if (newIndex < 0 || newIndex >= this.questionnaire.questions.length) {
                    return;
                }

                const orderedIds = this.questionnaire.questions.map(question => question.id);
                const [movedId] = orderedIds.splice(index, 1);
                orderedIds.splice(newIndex, 0, movedId);

                this.questionnaire.isSaving = true;
                this.questionnaire.errorMessage = "";
                this.questionnaire.errorDetails = [];

                try {
                    const response = await fetch(`${this.apiBaseUrl}/questions/reorder`, {
                        method: "POST",
                        credentials: "same-origin",
                        headers: {
                            "Accept": "application/json",
                            "Content-Type": "application/json",
                            [this.antiforgeryHeaderName]: antiforgeryToken
                        },
                        body: JSON.stringify({ orderedQuestionIds: orderedIds })
                    });

                    if (!response.ok) {
                        throw await extractErrorState(response);
                    }

                    const payload = await response.json();
                    const state = payload.data ?? {};
                    this.questionnaire.questions = state.questions ?? [];
                    this.questionnaire.canManageQuestions = (state.canManageQuestions ?? false) && bootstrap.canManageQuestions;
                } catch (error) {
                    this.questionnaire.errorMessage = error.message;
                    this.questionnaire.errorDetails = error.errors ?? [];
                } finally {
                    this.questionnaire.isSaving = false;
                }
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
            scoreLabel(score) {
                return score ?? 0;
            }
        }
    });

    app.config.compilerOptions.delimiters = ["[[", "]]"];
    app.mount(root);
})();
