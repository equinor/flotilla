import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { Task } from 'models/Task'
import { StyledInspection, StyledInspectionImage } from './InspectionStyles'
import { tokens } from '@equinor/eds-tokens'
import { CircularProgress, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { ValidInspectionReportInspectionTypes } from 'models/Inspection'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

const StyledSmallImagePlaceholder = styled.div`
    display: flex;
    justify-content: center;
    align-items: flex-start;
    padding: 16px 8px;
    height: 100%;
`
const StyledLargeImagePlaceholder = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 16px;
    gap: 16px;
`
const StyledCircularProgress = styled.div`
    display: flex;
    align-items: center;
    align-self: center;
    padding: 16px;
    height: 100%;
`

export const LargeImageErrorPlaceholder = ({ errorMessage }: { errorMessage: string }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledLargeImagePlaceholder>
            <Typography variant="h3" color={tokens.colors.text.static_icons__tertiary.hex}>
                {TranslateText(errorMessage)}
            </Typography>
        </StyledLargeImagePlaceholder>
    )
}

const SmallImageErrorPlaceholder = ({ errorMessage }: { errorMessage: string }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledSmallImagePlaceholder>
            <Typography color={tokens.colors.text.static_icons__tertiary.hex}>{TranslateText(errorMessage)}</Typography>
        </StyledSmallImagePlaceholder>
    )
}

export const LargeImagePendingPlaceholder = () => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledLargeImagePlaceholder>
            <Typography variant="h3" color={tokens.colors.text.static_icons__tertiary.hex}>
                {TranslateText('Waiting for inspection result')}
            </Typography>
            <CircularProgress size={48} />
        </StyledLargeImagePlaceholder>
    )
}

const SmallImagePendingPlaceholder = () => {
    return (
        <StyledCircularProgress>
            <CircularProgress />
        </StyledCircularProgress>
    )
}

const InspectionImageWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    const { fetchImageData } = useInspectionsContext()
    const { data, isPending, isError } = fetchImageData(task.inspection.isarInspectionId)

    if (!ValidInspectionReportInspectionTypes.includes(task.inspection.inspectionType)) {
        const errorMsg = 'Viewing of the inspection type is not supported'
        return isLargeImage ? (
            <LargeImageErrorPlaceholder errorMessage={errorMsg} />
        ) : (
            <SmallImageErrorPlaceholder errorMessage={errorMsg} />
        )
    } else if (isError) {
        const errorMsg = 'No inspection could be found'
        return isLargeImage ? (
            <LargeImageErrorPlaceholder errorMessage={errorMsg} />
        ) : (
            <SmallImageErrorPlaceholder errorMessage={errorMsg} />
        )
    } else if (isPending) {
        return isLargeImage ? <LargeImagePendingPlaceholder /> : <SmallImagePendingPlaceholder />
    } else
        return isLargeImage ? (
            <StyledInspection $otherContentHeight={'174px'} src={data} />
        ) : (
            <StyledInspectionImage src={data} />
        )
}

export const LargeDialogInspectionImage = ({ task }: { task: Task }) => {
    return <InspectionImageWithPlaceholder task={task} isLargeImage={true} />
}

export const SmallInspectionImage = ({ task }: { task: Task }) => {
    return <InspectionImageWithPlaceholder task={task} isLargeImage={false} />
}
