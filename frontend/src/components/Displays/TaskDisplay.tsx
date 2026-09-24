import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { AnalysisType } from 'models/MissionDefinition'
import { saraAnalysisTypeToEnum } from 'models/SaraAnalysisTypeMapping'

export const AnalysisValueDisplay = ({
    value,
    unit,
    analysisType,
    presentation = 'compact',
}: {
    value: string
    unit?: string
    analysisType?: string
    presentation?: 'compact' | 'result'
}) => {
    const { TranslateText } = useLanguageContext()
    const prominent = presentation === 'result'
    let label: string | undefined
    let formattedValue = prominent ? [value, unit?.trim()].filter(Boolean).join(' ') : `${value}${unit ?? ''}`

    switch (saraAnalysisTypeToEnum(analysisType)) {
        case AnalysisType.CLOE: {
            label = TranslateText('Level')
            const level = prominent ? Number(value.replace(',', '.')) : parseFloat(value)
            formattedValue =
                prominent && (!value.trim() || !Number.isFinite(level))
                    ? value
                    : `${Math.round(level * 100)}${prominent ? ' ' : ''}%`
            break
        }
        case AnalysisType.Fencilla: {
            label = TranslateText('Breach')
            const normalized = value.trim().toLowerCase()
            if (!prominent) {
                formattedValue = TranslateText(value.toLowerCase() === 'true' ? 'Finding' : 'No finding')
            } else if (normalized === 'true' || normalized === 'false') {
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
        <Typography variant={prominent ? 'h3' : undefined} as="p">
            {prominent && label && `${label}: `}
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
