import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { Task } from 'models/Task'
import { StyledInspection, StyledInspectionImage } from './InspectionStyles'
import { tokens } from '@equinor/eds-tokens'
import { CircularProgress, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { DisplayMethod, InspectionTypeToDisplayMethod } from 'models/Inspection'
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

export const TextAsImage = ({
    isLargeImage,
    text,
    textArgs,
}: {
    isLargeImage: boolean
    text: string
    textArgs?: string[]
}) => {
    const { TranslateText } = useLanguageContext()

    if (isLargeImage) {
        return (
            <StyledLargeImagePlaceholder>
                <Typography variant="h3" color={tokens.colors.text.static_icons__tertiary.hex}>
                    {TranslateText(text, textArgs)}
                </Typography>
            </StyledLargeImagePlaceholder>
        )
    }

    return (
        <StyledSmallImagePlaceholder>
            <Typography color={tokens.colors.text.static_icons__tertiary.hex}>
                {TranslateText(text, textArgs)}
            </Typography>
        </StyledSmallImagePlaceholder>
    )
}

export const PendingResultPlaceholder = ({ isLargeImage }: { isLargeImage: boolean }) => {
    const { TranslateText } = useLanguageContext()

    if (isLargeImage) {
        return (
            <StyledLargeImagePlaceholder>
                <Typography variant="h3" color={tokens.colors.text.static_icons__tertiary.hex}>
                    {TranslateText('Waiting for inspection result')}
                </Typography>
                <CircularProgress size={48} />
            </StyledLargeImagePlaceholder>
        )
    }

    return (
        <StyledCircularProgress>
            <CircularProgress />
        </StyledCircularProgress>
    )
}

const InspectionImageWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    const { fetchImageData } = useInspectionsContext()
    const { data, isPending, isError } = fetchImageData(task.inspection.isarInspectionId)
    if (isError || !data) {
        const errorMsg = 'No inspection could be found'
        return <TextAsImage isLargeImage={isLargeImage} text={errorMsg} />
    } else if (isPending) {
        return <PendingResultPlaceholder isLargeImage={isLargeImage} />
    } else
        return isLargeImage ? (
            <StyledInspection $otherContentHeight={'174px'} src={data} />
        ) : (
            <StyledInspectionImage src={data} />
        )
}

const InspectionValueWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    const { fetchValueData } = useInspectionsContext()
    const { data, isPending, isError } = fetchValueData(task.inspection.isarInspectionId)

    if (isError || data === undefined) {
        const errorMsg = 'No inspection could be found'
        return <TextAsImage isLargeImage={isLargeImage} text={errorMsg} />
    } else if (isPending) {
        return <PendingResultPlaceholder isLargeImage={isLargeImage} />
    } else {
        return <TextAsImage isLargeImage={isLargeImage} text={`CO2 consentration: {0}%`} textArgs={[data]} />
    }
}

const InspectionResultWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    if (InspectionTypeToDisplayMethod[task.inspection.inspectionType] === DisplayMethod.None) {
        const errorMsg = 'Viewing of the inspection type is not supported'
        return <TextAsImage isLargeImage={isLargeImage} text={errorMsg} />
    } else if (InspectionTypeToDisplayMethod[task.inspection.inspectionType] === DisplayMethod.Number) {
        return <InspectionValueWithPlaceholder task={task} isLargeImage={isLargeImage} />
    } else if (InspectionTypeToDisplayMethod[task.inspection.inspectionType] === DisplayMethod.Image) {
        return <InspectionImageWithPlaceholder task={task} isLargeImage={isLargeImage} />
    }
}

export const LargeDialogInspectionResult = ({ task }: { task: Task }) => {
    return <InspectionResultWithPlaceholder task={task} isLargeImage={true} />
}

export const SmallInspectionResult = ({ task }: { task: Task }) => {
    return <InspectionResultWithPlaceholder task={task} isLargeImage={false} />
}
