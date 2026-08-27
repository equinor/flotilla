import { Typography } from '@equinor/eds-core-react'
import { AnalysisType } from 'models/MissionDefinition'
import { saraAnalysisTypeToEnum } from 'models/SaraAnalysisTypeMapping'

export const AnalysisValueDisplay = ({
    value,
    unit,
    analysisType,
}: {
    value: string
    unit?: string
    analysisType?: string
}) => {
    if (saraAnalysisTypeToEnum(analysisType) === AnalysisType.CLOE) {
        return <Typography>{Math.round(parseFloat(value) * 100)}%</Typography>
    }
    return (
        <Typography>
            {value}
            {unit}
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
