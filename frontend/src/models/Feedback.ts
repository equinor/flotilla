export enum FeedbackType {
    BugReport = 'BugReport',
    FeatureRequest = 'FeatureRequest',
    Other = 'Other',
}

export interface FeedbackForm {
    title: string
    email: string
    shortName: string
    description: string
    type: FeedbackType | ''
}

export interface FeedbackQuery {
    title: string
    email: string
    shortName: string
    description: string
    type: FeedbackType
    timestamp: string
    url: string
}

export const FeedbackLimits = {
    title: 200,
    email: 50,
    shortName: 10,
    description: 3000,
} as const

type FeedbackField = keyof FeedbackForm
export type FeedbackErrors = Partial<Record<FeedbackField, string>>

const isValidEmail = (email: string) => /^[^\s@]+@[^\s@]+$/.test(email)

export const validateFeedback = (form: FeedbackForm): FeedbackErrors => {
    const values = {
        title: form.title.trim(),
        email: form.email.trim(),
        shortName: form.shortName.trim(),
        description: form.description.trim(),
    }
    const errors: FeedbackErrors = {}

    if (!form.type) errors.type = 'Feedback type is required'
    if (!values.title) errors.title = 'Title is required'
    else if (values.title.length > FeedbackLimits.title) errors.title = 'Title is too long'

    if (!values.description) errors.description = 'Description is required'
    else if (values.description.length > FeedbackLimits.description) errors.description = 'Description is too long'

    if (!values.email) errors.email = 'Email is required'
    else if (values.email.length > FeedbackLimits.email) errors.email = 'Email is too long'
    else if (!isValidEmail(values.email)) errors.email = 'Email is invalid'

    if (!values.shortName) errors.shortName = 'Short name is required'
    else if (values.shortName.length > FeedbackLimits.shortName) errors.shortName = 'Short name is too long'

    return errors
}

export const toFeedbackQuery = (
    form: FeedbackForm,
    timestamp = new Date(),
    url = window.location.href
): FeedbackQuery => ({
    title: form.title.trim(),
    email: form.email.trim(),
    shortName: form.shortName.trim(),
    description: form.description.trim(),
    type: form.type as FeedbackType,
    timestamp: timestamp.toISOString(),
    url,
})

interface FeedbackAccount {
    username: string
    idTokenClaims?: object
}

interface FeedbackIdentity {
    email: string
    shortName: string
    isEmailFromLogin: boolean
    isShortNameFromLogin: boolean
}

const getClaim = (claims: Record<string, unknown>, names: string[]) => {
    for (const name of names) {
        const value = claims[name]
        if (typeof value === 'string' && value.trim()) return value.trim()
    }
    return ''
}

export const resolveFeedbackIdentity = (account?: FeedbackAccount | null): FeedbackIdentity => {
    if (!account) return { email: '', shortName: '', isEmailFromLogin: false, isShortNameFromLogin: false }

    const claims = (account.idTokenClaims ?? {}) as Record<string, unknown>
    const email = getClaim(claims, ['email', 'mail', 'preferred_username', 'upn']) || account.username.trim()
    const shortName =
        getClaim(claims, ['shortName', 'shortname', 'short_name', 'onPremisesSamAccountName']) || email.split('@')[0]
    const errors = validateFeedback({
        title: 'valid',
        description: 'valid',
        type: FeedbackType.Other,
        email,
        shortName,
    })

    return {
        email,
        shortName,
        isEmailFromLogin: !errors.email,
        isShortNameFromLogin: !errors.shortName,
    }
}
