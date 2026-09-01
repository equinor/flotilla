import { Button, NativeSelect, Textarea, TextField, Typography } from '@equinor/eds-core-react'
import { useMsal } from '@azure/msal-react'
import { useBackendApi } from 'api/UseBackendApi'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { StyledDialog } from 'components/Styles/StyledComponents'
import {
    FeedbackErrors,
    FeedbackForm,
    FeedbackLimits,
    FeedbackType,
    resolveFeedbackIdentity,
    toFeedbackQuery,
    validateFeedback,
} from 'models/Feedback'
import { ChangeEvent, FormEvent, useState } from 'react'
import styled from 'styled-components'

const FeedbackFormContent = styled.form`
    display: flex;
    flex-direction: column;
    gap: 20px;
`

const FormFields = styled.div`
    display: flex;
    flex-direction: column;
    gap: 16px;
`

const FieldError = styled(Typography).attrs({ variant: 'caption' })`
    color: var(--eds-color-text-danger-strong);
    margin-top: -12px;
`

interface Props {
    isOpen: boolean
    onClose: () => void
}

const emptyForm: FeedbackForm = {
    type: '',
    title: '',
    description: '',
    email: '',
    shortName: '',
}

export const FeedbackDialog = ({ isOpen, onClose }: Props) => {
    const { instance, accounts } = useMsal()
    const backendApi = useBackendApi()
    const { TranslateText } = useLanguageContext()
    const account = instance.getActiveAccount() ?? accounts[0]
    const identity = resolveFeedbackIdentity(account)

    const [form, setForm] = useState<FeedbackForm>({
        ...emptyForm,
        email: identity.email,
        shortName: identity.shortName,
    })
    const [errors, setErrors] = useState<FeedbackErrors>({})
    const [isSubmitting, setIsSubmitting] = useState(false)
    const [submitFailed, setSubmitFailed] = useState(false)
    const [isSubmitted, setIsSubmitted] = useState(false)

    const updateField = (field: keyof FeedbackForm, value: string) => {
        setForm((current) => ({ ...current, [field]: value }))
        setErrors((current) => ({ ...current, [field]: undefined }))
        setSubmitFailed(false)
    }

    const submit = async (event: FormEvent) => {
        event.preventDefault()
        const validationErrors = validateFeedback(form)
        setErrors(validationErrors)
        if (Object.keys(validationErrors).length > 0) return

        setIsSubmitting(true)
        setSubmitFailed(false)
        try {
            await backendApi.submitFeedback(toFeedbackQuery(form))
            setIsSubmitted(true)
        } catch {
            setSubmitFailed(true)
        } finally {
            setIsSubmitting(false)
        }
    }

    const close = () => {
        if (!isSubmitting) onClose()
    }

    if (isSubmitted) {
        return (
            <StyledDialog open={isOpen} isDismissable onClose={close}>
                <StyledDialog.Header>
                    <StyledDialog.Title>
                        <Typography variant="h4">{TranslateText('Feedback sent')}</Typography>
                    </StyledDialog.Title>
                </StyledDialog.Header>
                <StyledDialog.CustomContent>
                    <Typography>{TranslateText('Thank you for helping us improve Flotilla.')}</Typography>
                </StyledDialog.CustomContent>
                <StyledDialog.Actions>
                    <Button onClick={close}>{TranslateText('Close')}</Button>
                </StyledDialog.Actions>
            </StyledDialog>
        )
    }

    return (
        <StyledDialog open={isOpen} isDismissable={!isSubmitting} onClose={close}>
            <FeedbackFormContent onSubmit={submit} noValidate>
                <StyledDialog.Header>
                    <StyledDialog.Title>
                        <Typography variant="h4">{TranslateText('Send feedback')}</Typography>
                    </StyledDialog.Title>
                </StyledDialog.Header>
                <StyledDialog.CustomContent>
                    <FormFields>
                        <NativeSelect
                            id="feedback-type"
                            label={TranslateText('Feedback type')}
                            value={form.type}
                            onChange={(event: ChangeEvent<HTMLSelectElement>) =>
                                updateField('type', event.target.value)
                            }
                            required
                        >
                            <option value="" disabled>
                                {TranslateText('Select feedback type')}
                            </option>
                            <option value={FeedbackType.BugReport}>{TranslateText('Bug report')}</option>
                            <option value={FeedbackType.FeatureRequest}>{TranslateText('Feature request')}</option>
                            <option value={FeedbackType.Other}>{TranslateText('Other')}</option>
                        </NativeSelect>
                        {errors.type && <FieldError>{TranslateText(errors.type)}</FieldError>}
                        <TextField
                            id="feedback-title"
                            label={TranslateText('Title')}
                            value={form.title}
                            maxLength={FeedbackLimits.title}
                            meta={`${form.title.length}/${FeedbackLimits.title}`}
                            variant={errors.title ? 'error' : undefined}
                            helperText={errors.title ? TranslateText(errors.title) : undefined}
                            onChange={(event: ChangeEvent<HTMLInputElement>) =>
                                updateField('title', event.target.value)
                            }
                            required
                        />
                        <Textarea
                            id="feedback-description"
                            label={TranslateText('Description')}
                            value={form.description}
                            maxLength={FeedbackLimits.description}
                            meta={`${form.description.length}/${FeedbackLimits.description}`}
                            rows={6}
                            variant={errors.description ? 'error' : undefined}
                            helperText={errors.description ? TranslateText(errors.description) : undefined}
                            onChange={(event: ChangeEvent<HTMLTextAreaElement>) =>
                                updateField('description', event.target.value)
                            }
                            required
                        />
                        <TextField
                            id="feedback-email"
                            type="email"
                            label={TranslateText('Email')}
                            value={form.email}
                            maxLength={FeedbackLimits.email}
                            readOnly={identity.isEmailFromLogin}
                            variant={errors.email ? 'error' : undefined}
                            helperText={errors.email ? TranslateText(errors.email) : undefined}
                            onChange={(event: ChangeEvent<HTMLInputElement>) =>
                                updateField('email', event.target.value)
                            }
                            required
                        />
                        <TextField
                            id="feedback-short-name"
                            label={TranslateText('Short name')}
                            value={form.shortName}
                            maxLength={FeedbackLimits.shortName}
                            readOnly={identity.isShortNameFromLogin}
                            variant={errors.shortName ? 'error' : undefined}
                            helperText={errors.shortName ? TranslateText(errors.shortName) : undefined}
                            onChange={(event: ChangeEvent<HTMLInputElement>) =>
                                updateField('shortName', event.target.value)
                            }
                            required
                        />
                        {submitFailed && <FieldError>{TranslateText('Failed to send feedback')}</FieldError>}
                    </FormFields>
                </StyledDialog.CustomContent>
                <StyledDialog.Actions>
                    <Button type="button" variant="ghost" onClick={close} disabled={isSubmitting}>
                        {TranslateText('Cancel')}
                    </Button>
                    <Button type="submit" disabled={isSubmitting}>
                        {isSubmitting ? TranslateText('Sending feedback') : TranslateText('Send feedback')}
                    </Button>
                </StyledDialog.Actions>
            </FeedbackFormContent>
        </StyledDialog>
    )
}
