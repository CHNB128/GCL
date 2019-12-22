
(def *host-language* "c#")

(def not (fn [a] (if a false true)))

(def load-file (fn [f] (eval (read-string (str "(do " (slurp f) ")")))))

(defmacro! cond
  (fn (& xs)
    (if (> (count xs) 0)
      (list
        'if (first xs)
          (if (> (count xs) 1)
            (nth xs 1)
            (throw "odd number of forms to cond"))
          (cons 'cond (rest (rest xs)))))))

(def *gensym-counter*
  (atom 0))

(def gensym
  (fn []
    (symbol (str "G__" (swap! *gensym-counter* (fn [x] (+ 1 x)))))))

(defmacro or
  (fn [& xs]
    (if (empty? xs)
      nil
      (if (= 1 (count xs))
        (first xs)
        (let (condvar (gensym))
          `(let (~condvar ~(first xs))
            (if ~condvar
              ~condvar
              (or ~@(rest xs)))))))))