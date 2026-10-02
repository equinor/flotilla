import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { chevron_left, chevron_right, close } from '@equinor/eds-icons'
import { useLanguageContext } from 'contexts/LanguageContext'
import { KeyboardEvent, useEffect, useId, useRef } from 'react'
import { getResultFocus, MissionResult, ResultFocus } from './missionResultPresentation'
import { ResultMediaView } from './ResultMediaView'
import { GalleryResultContent } from './GalleryResultContent'
import {
    Gallery,
    GalleryBody,
    GalleryDescription,
    GalleryDetails,
    GalleryHeader,
    GalleryHeading,
    GalleryTaskNumber,
    Navigation,
    ResultTitle,
    Strip,
    StripButton,
} from './MissionResultStyles'

interface Props {
    results: MissionResult[]
    selectedId: string
    preferredFocus: ResultFocus
    installationName: string
    robotName: string | undefined
    loadFailed: boolean
    onSelect: (id: string, focus?: ResultFocus) => void
    onClose: () => void
    returnFocus: () => void
}

export const MissionResultGallery = ({
    results,
    selectedId,
    preferredFocus,
    installationName,
    robotName,
    loadFailed,
    onSelect,
    onClose,
    returnFocus,
}: Props) => {
    const { TranslateText } = useLanguageContext()
    const titleId = useId()
    const dialog = useRef<HTMLDialogElement>(null)
    const heading = useRef<HTMLHeadingElement>(null)
    const activeThumbnail = useRef<HTMLButtonElement>(null)
    const restoreFocus = useRef(returnFocus)
    const index = results.findIndex((result) => result.inspection.inspectionId === selectedId)
    const result = results[index]
    const focus = result ? getResultFocus(result, preferredFocus) : preferredFocus
    const inspection = result?.inspection

    useEffect(() => {
        const element = dialog.current
        if (!element) return
        element.setAttribute('aria-labelledby', titleId)
        const onCancel = (event: Event) => {
            if (event.target !== element) return
            event.preventDefault()
            onClose()
        }
        element.addEventListener('cancel', onCancel)
        return () => element.removeEventListener('cancel', onCancel)
    }, [titleId, onClose])

    useEffect(() => {
        const thumbnail = activeThumbnail.current
        const strip = thumbnail?.parentElement
        if (!thumbnail || !strip) return
        const itemBounds = thumbnail.getBoundingClientRect()
        const stripBounds = strip.getBoundingClientRect()
        if (itemBounds.left < stripBounds.left) strip.scrollLeft += itemBounds.left - stripBounds.left
        else if (itemBounds.right > stripBounds.right) strip.scrollLeft += itemBounds.right - stripBounds.right
    }, [selectedId, index])

    useEffect(
        () => () => {
            requestAnimationFrame(() => restoreFocus.current())
        },
        []
    )

    const navigate = (offset: number) => {
        const next = results[index + offset]
        if (index !== -1 && next) onSelect(next.inspection.inspectionId, preferredFocus)
    }

    const onKeyDown = (event: KeyboardEvent<HTMLElement>) => {
        const target = event.target
        if (!(target instanceof HTMLElement)) return
        if (target.closest('dialog') !== event.currentTarget.closest('dialog')) {
            event.stopPropagation()
            return
        }
        if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return
        if (target.closest('video, audio, input, textarea, select, [contenteditable="true"]')) return
        if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
            event.preventDefault()
            navigate(event.key === 'ArrowLeft' ? -1 : 1)
        }
    }

    return (
        <Gallery
            open
            isDismissable
            dialogRef={dialog}
            onClose={onClose}
            aria-labelledby={titleId}
            aria-describedby={undefined}
            onKeyDown={onKeyDown}
            onMouseDown={(event) => {
                if (
                    event.target instanceof HTMLElement &&
                    event.target.closest('dialog') !== event.currentTarget.closest('dialog')
                ) {
                    event.stopPropagation()
                }
            }}
        >
            <GalleryHeader>
                <GalleryDetails>
                    <GalleryHeading>
                        <ResultTitle variant="h3" id={titleId} ref={heading} tabIndex={-1}>
                            {inspection?.tag || TranslateText('Inspection results')}
                        </ResultTitle>
                        {result && (
                            <GalleryTaskNumber variant="caption">
                                {TranslateText('Result task {0}', [String(result.taskNumber)])}
                            </GalleryTaskNumber>
                        )}
                    </GalleryHeading>
                    {inspection?.inspectionDescription && (
                        <GalleryDescription variant="body_long">{inspection.inspectionDescription}</GalleryDescription>
                    )}
                </GalleryDetails>
                <Navigation>
                    {index !== -1 && (
                        <Typography variant="caption">
                            {index + 1} / {results.length}
                        </Typography>
                    )}
                    <Button
                        variant="ghost_icon"
                        aria-label={TranslateText('Previous inspection')}
                        disabled={index <= 0}
                        onClick={() => navigate(-1)}
                    >
                        <Icon data={chevron_left} />
                    </Button>
                    <Button
                        variant="ghost_icon"
                        aria-label={TranslateText('Next inspection')}
                        disabled={index === -1 || index === results.length - 1}
                        onClick={() => navigate(1)}
                    >
                        <Icon data={chevron_right} />
                    </Button>
                    <Button variant="ghost_icon" aria-label={TranslateText('Close result gallery')} onClick={onClose}>
                        <Icon data={close} />
                    </Button>
                </Navigation>
            </GalleryHeader>
            {loadFailed && (
                <GalleryBody $paired={false}>
                    <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>
                </GalleryBody>
            )}
            {result ? (
                <GalleryResultContent
                    result={result}
                    focus={focus}
                    installationName={installationName}
                    robotName={robotName}
                    onFocus={(next) => {
                        heading.current?.focus({ preventScroll: true })
                        onSelect(selectedId, next)
                    }}
                />
            ) : !loadFailed ? (
                <GalleryBody $paired={false}>
                    <Typography role="alert">{TranslateText('Selected result is unavailable')}</Typography>
                </GalleryBody>
            ) : null}
            <Strip aria-label={TranslateText('Inspections in this mission')}>
                <Typography variant="h6">{TranslateText('Inspections in this mission')}</Typography>
                <div>
                    {results.map((item) => {
                        const id = item.inspection.inspectionId
                        const thumbnail = item.source ?? item.analysis
                        return (
                            <StripButton
                                key={id}
                                $hasFinding={item.hasAnalysis ? item.hasFinding : undefined}
                                ref={id === selectedId ? activeThumbnail : undefined}
                                variant="ghost"
                                aria-current={id === selectedId ? 'true' : undefined}
                                aria-label={TranslateText('View result for {0}, task {1}', [
                                    item.inspection.tag,
                                    String(item.taskNumber),
                                ])}
                                onClick={() => onSelect(id, preferredFocus)}
                            >
                                {thumbnail && (
                                    <ResultMediaView media={thumbnail} label={item.inspection.tag} size="thumbnail" />
                                )}
                                <Typography variant="body_short">{item.inspection.tag}</Typography>
                                <Typography variant="caption">
                                    {TranslateText('Result task {0}', [String(item.taskNumber)])}
                                </Typography>
                            </StripButton>
                        )
                    })}
                </div>
            </Strip>
        </Gallery>
    )
}
