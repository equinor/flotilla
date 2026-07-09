import { Typography } from '@equinor/eds-core-react'

export const TagIdDisplay = ({ tagId, index }: { tagId: string | undefined; index: number }) => {
    if (!tagId) return <Typography key={index + 'tagId'}>{'N/A'}</Typography>
    else return <Typography key={index + 'tagId'}>{tagId!}</Typography>
}

export const DescriptionDisplay = ({ description, index }: { description: string | undefined; index: number }) => {
    if (!description) return <Typography key={index + 'descr'}>{'N/A'}</Typography>
    return <Typography key={index + 'descr'}>{description}</Typography>
}
