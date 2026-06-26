import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { Task } from 'models/Task'
import { StyledInspection, StyledInspectionImage } from './InspectionStyles'
import { tokens } from '@equinor/eds-tokens'
import { CircularProgress, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { DisplayMethod, SensorTypeToDisplayMethod } from 'models/Inspection'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { VideoPlaceholder, VideoPlayer } from './InspectionVideoPlayer'

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

export const InspectionImageWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    const { useMediaData } = useInspectionsContext()
    const { data, isPending, isError } = useMediaData(task.inspection.isarInspectionId)
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
    const { useValueData } = useInspectionsContext()
    const { data, isPending, isError } = useValueData(task.inspection.isarInspectionId)

    if (isError || data === undefined) {
        const errorMsg = 'No inspection could be found'
        return <TextAsImage isLargeImage={isLargeImage} text={errorMsg} />
    } else if (isPending) {
        return <PendingResultPlaceholder isLargeImage={isLargeImage} />
    } else {
        return (
            <TextAsImage
                isLargeImage={isLargeImage}
                text={`CO2 consentration: {0}%`}
                textArgs={[data.toFixed(3).toString()]}
            />
        )
    }
}

const InspectionVideoWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    return isLargeImage ? <LargeVideoWithPlaceholder task={task} /> : <SmallVideoWithPlaceholder task={task} />
}

const LargeVideoWithPlaceholder = ({ task }: { task: Task }) => {
    const { useMediaData } = useInspectionsContext()
    const { data, isPending, isError } = useMediaData(task.inspection.isarInspectionId)
    if (isPending) {
        return <PendingResultPlaceholder isLargeImage={true} />
    } else if (isError || !data) {
        return <TextAsImage isLargeImage={true} text={'No inspection could be found'} />
    } else return <VideoPlayer src={data} />
}

const SmallVideoWithPlaceholder = ({ task }: { task: Task }) => {
    const { useMediaData } = useInspectionsContext()
    const { data, isPending, isError } = useMediaData(task.inspection.isarInspectionId)
    if (isPending) {
        return <PendingResultPlaceholder isLargeImage={false} />
    } else if (isError || !data) {
        return <TextAsImage isLargeImage={false} text={'No inspection could be found'} />
    } else return <VideoPlaceholder />
}

const InspectionResultWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    const displayMethod = SensorTypeToDisplayMethod[task.inspection.inspectionType]
    if (displayMethod === DisplayMethod.None) {
        const errorMsg = 'Viewing of the inspection type is not supported'
        return <TextAsImage isLargeImage={isLargeImage} text={errorMsg} />
    } else if (displayMethod === DisplayMethod.Number) {
        return <InspectionValueWithPlaceholder task={task} isLargeImage={isLargeImage} />
    } else if (displayMethod === DisplayMethod.Image) {
        return <InspectionImageWithPlaceholder task={task} isLargeImage={isLargeImage} />
    } else if (displayMethod === DisplayMethod.Video) {
        return <InspectionVideoWithPlaceholder task={task} isLargeImage={isLargeImage} />
    }
}

export const LargeDialogInspectionResult = ({ task }: { task: Task }) => {
    return <InspectionResultWithPlaceholder task={task} isLargeImage={true} />
}

export const SmallInspectionResult = ({ task }: { task: Task }) => {
    return <InspectionResultWithPlaceholder task={task} isLargeImage={false} />
}

const AnalysisImageWithPlaceholder = ({ task, isLargeImage }: { task: Task; isLargeImage: boolean }) => {
    const { useAnalysisData } = useInspectionsContext()
    const { data, isPending, isError } = useAnalysisData(task.inspection.isarInspectionId)
    if (!task.inspection.analysisResult?.storageAccount) {
        return <TextAsImage isLargeImage={isLargeImage} text={'No analysis available'} />
    }
    if (isError || !data) {
        return <TextAsImage isLargeImage={isLargeImage} text={'No analysis could be found'} />
    } else if (isPending) {
        return <PendingResultPlaceholder isLargeImage={isLargeImage} />
    } else
        return isLargeImage ? (
            <StyledInspection $otherContentHeight={'174px'} src={data} />
        ) : (
            <StyledInspectionImage src={data} />
        )
}

export const SmallAnalysisResult = ({ task }: { task: Task }) => {
    return <AnalysisImageWithPlaceholder task={task} isLargeImage={false} />
}
