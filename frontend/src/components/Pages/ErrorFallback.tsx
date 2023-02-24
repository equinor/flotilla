export function ErrorFallback(error: Error) {
    return (
        <>
            Type = {error.name} - {error.message} - {error.cause} 
        </>
    )
}
