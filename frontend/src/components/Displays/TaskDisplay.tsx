import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { AnalysisType } from 'models/MissionDefinition'
import { saraAnalysisTypeToEnum } from 'models/SaraAnalysisTypeMapping'

export const AnalysisValueDisplay = ({
    value,
    unit,
    analysisType,
    isCompact = true,
}: {
    value: string
    unit?: string
    analysisType?: string
    isCompact?: boolean
}) => {
    const { TranslateText } = useLanguageContext()
    const type = saraAnalysisTypeToEnum(analysisType)

    if (isCompact) {
        if (type === AnalysisType.CLOE) {
            return <Typography as="p">{`${Math.round(parseFloat(value) * 100)}%`}</Typography>
        }
        if (type === AnalysisType.Fencilla) {
            return (
                <Typography as="p">
                    {TranslateText(value.toLowerCase() === 'true' ? 'Finding' : 'No finding')}
                </Typography>
            )
        }
        return <Typography as="p">{`${value}${unit ?? ''}`}</Typography>
    }

    let label: string | undefined
    let formattedValue = [value, unit?.trim()].filter(Boolean).join(' ')

    switch (type) {
        case AnalysisType.CLOE: {
            label = TranslateText('Level')
            const level = Number(value.replace(',', '.'))
            formattedValue = !value.trim() || !Number.isFinite(level) ? value : `${Math.round(level * 100)} %`
            break
        }
        case AnalysisType.Fencilla: {
            label = TranslateText('Breach')
            const normalized = value.trim().toLowerCase()
            if (normalized === 'true' || normalized === 'false') {
                formattedValue = TranslateText(normalized === 'true' ? 'True' : 'False')
            } else {
                formattedValue = value
            }
            break
        }
        case AnalysisType.ThermalReading:
            label = TranslateText('Temperature')
            break
    }

    return (
        <Typography variant="h3" as="p">
            {label && `${label}: `}
            {formattedValue}
        </Typography>
    )
}

export const TagIdDisplay = ({ tagId, index }: { tagId: string | undefined; index: number }) => {
    if (!tagId) return <Typography key={index + 'tagId'}>{'N/A'}</Typography>
    else return <Typography key={index + 'tagId'}>{tagId!}</Typography>
}

export const DescriptionDisplay = ({ description, index }: { description: string | undefined; index: number }) => {
    if (!description) return <Typography key={index + 'descr'}>{'N/A'}</Typography>
    return <Typography key={index + 'descr'}>{description}</Typography>
}
